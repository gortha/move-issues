// =============================================================================
// Infrastructure/Caching/ICacheService.cs
// =============================================================================
namespace Infrastructure.Caching;

/// <summary>
/// Cluster-safe cache abstraction.
/// IMPORTANT: In Redis Cluster mode, native multi-key commands (MGET/MSET) require
/// all keys to map to the SAME hash slot, or you get a CROSSSLOT error. Since our
/// keys are NOT co-located via hash tags ({...}), bulk operations here use
/// pipelining (fire multiple single-key commands over the same connection without
/// waiting for each reply) instead of MGET/MSET. Pipelining is cluster-agnostic:
/// StackExchange.Redis transparently routes each command to the right node.
/// </summary>
public interface ICacheService
{
    Task<T?> GetByIdAsync<T>(string key, CancellationToken ct = default);

    Task<IReadOnlyDictionary<string, T?>> GetByIdsAsync<T>(
        IEnumerable<string> keys, CancellationToken ct = default);

    Task SetByIdAsync<T>(
        string key, T value, TimeSpan? ttl = null, CancellationToken ct = default);

    Task SetByIdsAsync<T>(
        IReadOnlyDictionary<string, T> items, TimeSpan? ttl = null, CancellationToken ct = default);

    /// <summary>
    /// Scans all master nodes for keys matching <paramref name="pattern"/> and returns
    /// their deserialized values. Uses SCAN (never KEYS) to avoid blocking nodes.
    /// Expensive — prefer GetByIdsAsync when you already know the key set.
    /// </summary>
    Task<IReadOnlyDictionary<string, T?>> GetAllAsync<T>(
        string pattern, CancellationToken ct = default);

    Task SetAllAsync<T>(
        IReadOnlyDictionary<string, T> items, TimeSpan? ttl = null, CancellationToken ct = default);
}

// =============================================================================
// Infrastructure/Caching/CacheOptions.cs
// =============================================================================
namespace Infrastructure.Caching;

public sealed class CacheOptions
{
    public const string SectionName = "Redis";

    public string ConnectionString { get; set; } = default!;

    /// <summary>Logical prefix applied to every key (namespace isolation, versioning).</summary>
    public string KeyPrefix { get; set; } = "app:v1:";

    public TimeSpan DefaultTtl { get; set; } = TimeSpan.FromMinutes(30);

    /// <summary>
    /// Max number of commands per pipeline batch. Keeps single round-trips bounded
    /// and avoids building an oversized in-memory command queue for huge key sets.
    /// </summary>
    public int MaxBatchSize { get; set; } = 500;

    /// <summary>SCAN COUNT hint per iteration for GetAllAsync.</summary>
    public int ScanPageSize { get; set; } = 250;
}

// =============================================================================
// Infrastructure/Caching/MessagePackCacheSerializer.cs
// =============================================================================
namespace Infrastructure.Caching;

using System.Buffers;
using MessagePack;
using MessagePack.Resolvers;

/// <summary>
/// Serialization boundary: MessagePack + LZ4Block compression, ArrayPool-backed
/// buffers to keep large payloads (multi-MB) off the Large Object Heap.
/// Reused as-is from the existing cache wrapper conventions.
/// </summary>
public sealed class MessagePackCacheSerializer
{
    private static readonly MessagePackSerializerOptions Options = MessagePackSerializerOptions
        .Standard
        .WithResolver(ContractlessStandardResolver.Instance)
        .WithCompression(MessagePackCompression.Lz4BlockArray);

    public byte[] Serialize<T>(T value)
    {
        var bufferWriter = new ArrayBufferWriter<byte>();
        MessagePackSerializer.Serialize(bufferWriter, value, Options);
        return bufferWriter.WrittenSpan.ToArray();
    }

    public T? Deserialize<T>(ReadOnlyMemory<byte> bytes)
    {
        if (bytes.IsEmpty) return default;
        return MessagePackSerializer.Deserialize<T>(bytes, Options);
    }
}

// =============================================================================
// Infrastructure/Caching/RedisClusterCacheService.cs
// =============================================================================
namespace Infrastructure.Caching;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

public sealed class RedisClusterCacheService : ICacheService
{
    private readonly IConnectionMultiplexer _connection;
    private readonly MessagePackCacheSerializer _serializer;
    private readonly CacheOptions _options;
    private readonly ILogger<RedisClusterCacheService> _logger;

    public RedisClusterCacheService(
        IConnectionMultiplexer connection,
        MessagePackCacheSerializer serializer,
        IOptions<CacheOptions> options,
        ILogger<RedisClusterCacheService> logger)
    {
        _connection = connection;
        _serializer = serializer;
        _options = options.Value;
        _logger = logger;
    }

    private IDatabase Db => _connection.GetDatabase();

    private string BuildKey(string id) => $"{_options.KeyPrefix}{id}";

    // -------------------------------------------------------------------
    // GetById
    // -------------------------------------------------------------------
    public async Task<T?> GetByIdAsync<T>(string key, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        RedisValue raw;
        try
        {
            raw = await Db.StringGetAsync(BuildKey(key)).ConfigureAwait(false);
        }
        catch (RedisConnectionException ex)
        {
            // Fail open: a cache outage should not take down the caller.
            _logger.LogWarning(ex, "Redis GET failed for key {Key}, falling through as cache miss", key);
            return default;
        }

        if (raw.IsNullOrEmpty) return default;

        return _serializer.Deserialize<T>(raw);
    }

    // -------------------------------------------------------------------
    // GetByIds — pipelined, cluster-safe (no MGET)
    // -------------------------------------------------------------------
    public async Task<IReadOnlyDictionary<string, T?>> GetByIdsAsync<T>(
        IEnumerable<string> keys, CancellationToken ct = default)
    {
        var keyList = keys.Distinct().ToList();
        var result = new Dictionary<string, T?>(keyList.Count);
        if (keyList.Count == 0) return result;

        foreach (var chunk in Chunk(keyList, _options.MaxBatchSize))
        {
            ct.ThrowIfCancellationRequested();

            var batch = Db.CreateBatch();
            var tasks = chunk
                .Select(id => (Id: id, Task: batch.StringGetAsync(BuildKey(id))))
                .ToList();

            batch.Execute();

            await Task.WhenAll(tasks.Select(t => t.Task)).ConfigureAwait(false);

            foreach (var (id, task) in tasks)
            {
                var raw = task.Result;
                result[id] = raw.IsNullOrEmpty ? default : _serializer.Deserialize<T>(raw);
            }
        }

        return result;
    }

    // -------------------------------------------------------------------
    // SetById
    // -------------------------------------------------------------------
    public async Task SetByIdAsync<T>(
        string key, T value, TimeSpan? ttl = null, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        var payload = _serializer.Serialize(value);

        try
        {
            await Db.StringSetAsync(BuildKey(key), payload, ttl ?? _options.DefaultTtl)
                .ConfigureAwait(false);
        }
        catch (RedisConnectionException ex)
        {
            _logger.LogWarning(ex, "Redis SET failed for key {Key}", key);
            // Swallow: caching failures should be non-fatal for the write path.
        }
    }

    // -------------------------------------------------------------------
    // SetByIds — pipelined, cluster-safe (no MSET)
    // -------------------------------------------------------------------
    public async Task SetByIdsAsync<T>(
        IReadOnlyDictionary<string, T> items, TimeSpan? ttl = null, CancellationToken ct = default)
    {
        if (items.Count == 0) return;
        var effectiveTtl = ttl ?? _options.DefaultTtl;

        foreach (var chunk in Chunk(items.ToList(), _options.MaxBatchSize))
        {
            ct.ThrowIfCancellationRequested();

            var batch = Db.CreateBatch();
            var tasks = chunk
                .Select(kv => batch.StringSetAsync(
                    BuildKey(kv.Key), _serializer.Serialize(kv.Value), effectiveTtl))
                .ToList();

            batch.Execute();

            await Task.WhenAll(tasks).ConfigureAwait(false);
        }
    }

    // -------------------------------------------------------------------
    // GetAll — SCAN across every master node (cluster-aware)
    // -------------------------------------------------------------------
    public async Task<IReadOnlyDictionary<string, T?>> GetAllAsync<T>(
        string pattern, CancellationToken ct = default)
    {
        var matchedKeys = new List<string>();
        var fullPattern = $"{_options.KeyPrefix}{pattern}";

        // In cluster mode, data is sharded across masters — SCAN only sees the
        // node it targets, so we must enumerate every master endpoint.
        foreach (var endpoint in _connection.GetEndPoints())
        {
            var server = _connection.GetServer(endpoint);
            if (server.IsReplica) continue;

            await foreach (var redisKey in server
                .KeysAsync(pattern: fullPattern, pageSize: _options.ScanPageSize)
                .WithCancellation(ct))
            {
                matchedKeys.Add(redisKey.ToString());
            }
        }

        if (matchedKeys.Count == 0)
            return new Dictionary<string, T?>();

        // Strip prefix back off before returning to the caller, then reuse the
        // pipelined bulk-get path.
        var idsWithoutPrefix = matchedKeys
            .Select(k => k.StartsWith(_options.KeyPrefix, StringComparison.Ordinal)
                ? k[_options.KeyPrefix.Length..]
                : k)
            .ToList();

        return await GetByIdsAsync<T>(idsWithoutPrefix, ct).ConfigureAwait(false);
    }

    // -------------------------------------------------------------------
    // SetAll — alias over SetByIds for symmetry with GetAll
    // -------------------------------------------------------------------
    public Task SetAllAsync<T>(
        IReadOnlyDictionary<string, T> items, TimeSpan? ttl = null, CancellationToken ct = default)
        => SetByIdsAsync(items, ttl, ct);

    // -------------------------------------------------------------------
    private static IEnumerable<List<T>> Chunk<T>(List<T> source, int size)
    {
        for (var i = 0; i < source.Count; i += size)
            yield return source.GetRange(i, Math.Min(size, source.Count - i));
    }
}

// =============================================================================
// Infrastructure/Caching/ServiceCollectionExtensions.cs
// =============================================================================
namespace Infrastructure.Caching;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;

public static class CacheServiceCollectionExtensions
{
    public static IServiceCollection AddRedisClusterCache(
        this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<CacheOptions>(configuration.GetSection(CacheOptions.SectionName));

        services.AddSingleton<IConnectionMultiplexer>(sp =>
        {
            var options = sp.GetRequiredService<IOptions<CacheOptions>>().Value;

            var configOptions = ConfigurationOptions.Parse(options.ConnectionString);
            configOptions.AbortOnConnectFail = false; // resilient startup
            configOptions.ConnectRetry = 3;
            configOptions.ConnectTimeout = 5000;
            configOptions.SyncTimeout = 5000;
            // configOptions.Password / Ssl / Tls settings go here for prod

            return ConnectionMultiplexer.Connect(configOptions);
        });

        services.AddSingleton<MessagePackCacheSerializer>();
        services.AddSingleton<ICacheService, RedisClusterCacheService>();

        // Example: layer the existing Decorator pattern on top for a specific
        // domain service, e.g.:
        // services.Decorate<IClientSearchService, CachedClientSearchService>();

        return services;
    }
}
