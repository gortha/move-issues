using System;

namespace YourNamespace.Models
{
    /// <summary>
    /// Model for rendering Outlook-compatible email badges with border radius
    /// </summary>
    public class EmailBadgeModel
    {
        /// <summary>
        /// The text displayed in the badge
        /// </summary>
        public string Text { get; set; }

        /// <summary>
        /// Optional URL if badge should be clickable
        /// </summary>
        public string Url { get; set; }

        /// <summary>
        /// Background color (hex format, e.g., "#dc3545")
        /// Default: #6c757d
        /// </summary>
        public string BackgroundColor { get; set; }

        /// <summary>
        /// Text color (hex format, e.g., "#ffffff")
        /// Default: #ffffff
        /// </summary>
        public string TextColor { get; set; }

        /// <summary>
        /// Badge height in pixels
        /// Default: 24
        /// </summary>
        public int? Height { get; set; }

        /// <summary>
        /// Border radius in pixels
        /// Default: 12
        /// </summary>
        public int? BorderRadius { get; set; }

        /// <summary>
        /// Font size in pixels
        /// Default: 12
        /// </summary>
        public int? FontSize { get; set; }

        /// <summary>
        /// Font family
        /// Default: Arial, sans-serif
        /// </summary>
        public string FontFamily { get; set; }

        /// <summary>
        /// Font weight (e.g., "bold", "normal", "600")
        /// Default: bold
        /// </summary>
        public string FontWeight { get; set; }

        /// <summary>
        /// Horizontal padding in pixels
        /// Default: 12
        /// </summary>
        public int? PaddingHorizontal { get; set; }

        /// <summary>
        /// Border color for outlined badges (hex format)
        /// If null, no border is applied
        /// </summary>
        public string BorderColor { get; set; }

        /// <summary>
        /// Border width in pixels (for outlined badges)
        /// Default: 1
        /// </summary>
        public int? BorderWidth { get; set; }

        /// <summary>
        /// CSS margin (e.g., "0 5px 0 0")
        /// Default: 0
        /// </summary>
        public string Margin { get; set; }

        /// <summary>
        /// CSS display property (inline-block, block)
        /// Default: inline-block
        /// </summary>
        public string Display { get; set; }

        /// <summary>
        /// Creates a default primary badge
        /// </summary>
        public static EmailBadgeModel CreatePrimary(string text)
        {
            return new EmailBadgeModel
            {
                Text = text,
                BackgroundColor = "#007bff",
                TextColor = "#ffffff",
                Height = 24,
                BorderRadius = 12,
                FontSize = 12,
                PaddingHorizontal = 12
            };
        }

        /// <summary>
        /// Creates a success badge
        /// </summary>
        public static EmailBadgeModel CreateSuccess(string text)
        {
            return new EmailBadgeModel
            {
                Text = text,
                BackgroundColor = "#28a745",
                TextColor = "#ffffff",
                Height = 24,
                BorderRadius = 12,
                FontSize = 12,
                PaddingHorizontal = 12
            };
        }

        /// <summary>
        /// Creates a danger badge
        /// </summary>
        public static EmailBadgeModel CreateDanger(string text)
        {
            return new EmailBadgeModel
            {
                Text = text,
                BackgroundColor = "#dc3545",
                TextColor = "#ffffff",
                Height = 24,
                BorderRadius = 12,
                FontSize = 12,
                PaddingHorizontal = 12
            };
        }

        /// <summary>
        /// Creates a warning badge
        /// </summary>
        public static EmailBadgeModel CreateWarning(string text)
        {
            return new EmailBadgeModel
            {
                Text = text,
                BackgroundColor = "#ffc107",
                TextColor = "#212529",
                Height = 24,
                BorderRadius = 12,
                FontSize = 12,
                PaddingHorizontal = 12
            };
        }

        /// <summary>
        /// Creates an info badge
        /// </summary>
        public static EmailBadgeModel CreateInfo(string text)
        {
            return new EmailBadgeModel
            {
                Text = text,
                BackgroundColor = "#17a2b8",
                TextColor = "#ffffff",
                Height = 24,
                BorderRadius = 12,
                FontSize = 12,
                PaddingHorizontal = 12
            };
        }

        /// <summary>
        /// Creates a secondary badge
        /// </summary>
        public static EmailBadgeModel CreateSecondary(string text)
        {
            return new EmailBadgeModel
            {
                Text = text,
                BackgroundColor = "#6c757d",
                TextColor = "#ffffff",
                Height = 24,
                BorderRadius = 12,
                FontSize = 12,
                PaddingHorizontal = 12
            };
        }

        /// <summary>
        /// Creates a light badge
        /// </summary>
        public static EmailBadgeModel CreateLight(string text)
        {
            return new EmailBadgeModel
            {
                Text = text,
                BackgroundColor = "#f8f9fa",
                TextColor = "#212529",
                Height = 24,
                BorderRadius = 12,
                FontSize = 12,
                PaddingHorizontal = 12
            };
        }

        /// <summary>
        /// Creates a dark badge
        /// </summary>
        public static EmailBadgeModel CreateDark(string text)
        {
            return new EmailBadgeModel
            {
                Text = text,
                BackgroundColor = "#343a40",
                TextColor = "#ffffff",
                Height = 24,
                BorderRadius = 12,
                FontSize = 12,
                PaddingHorizontal = 12
            };
        }

        /// <summary>
        /// Creates an outlined badge
        /// </summary>
        public static EmailBadgeModel CreateOutlined(string text, string color = "#007bff")
        {
            return new EmailBadgeModel
            {
                Text = text,
                BackgroundColor = "#ffffff",
                TextColor = color,
                BorderColor = color,
                BorderWidth = 1,
                Height = 24,
                BorderRadius = 12,
                FontSize = 12,
                PaddingHorizontal = 12
            };
        }

        /// <summary>
        /// Creates a pill-shaped badge (full radius)
        /// </summary>
        public static EmailBadgeModel CreatePill(string text, string backgroundColor = "#007bff")
        {
            return new EmailBadgeModel
            {
                Text = text,
                BackgroundColor = backgroundColor,
                TextColor = "#ffffff",
                Height = 24,
                BorderRadius = 50, // Full pill shape
                FontSize = 12,
                PaddingHorizontal = 12
            };
        }

        /// <summary>
        /// Creates a small badge
        /// </summary>
        public static EmailBadgeModel CreateSmall(string text, string backgroundColor = "#6c757d")
        {
            return new EmailBadgeModel
            {
                Text = text,
                BackgroundColor = backgroundColor,
                TextColor = "#ffffff",
                Height = 20,
                BorderRadius = 10,
                FontSize = 10,
                PaddingHorizontal = 8
            };
        }

        /// <summary>
        /// Creates a large badge
        /// </summary>
        public static EmailBadgeModel CreateLarge(string text, string backgroundColor = "#007bff")
        {
            return new EmailBadgeModel
            {
                Text = text,
                BackgroundColor = backgroundColor,
                TextColor = "#ffffff",
                Height = 32,
                BorderRadius = 16,
                FontSize = 14,
                PaddingHorizontal = 16
            };
        }
    }
}
