using System;

namespace YourNamespace.Models
{
    /// <summary>
    /// Model for rendering Outlook-compatible email buttons with border radius
    /// </summary>
    public class EmailButtonModel
    {
        /// <summary>
        /// The URL the button links to
        /// </summary>
        public string Url { get; set; }

        /// <summary>
        /// The text displayed on the button
        /// </summary>
        public string Text { get; set; }

        /// <summary>
        /// Background color (hex format, e.g., "#007bff")
        /// Default: #007bff
        /// </summary>
        public string BackgroundColor { get; set; }

        /// <summary>
        /// Text color (hex format, e.g., "#ffffff")
        /// Default: #ffffff
        /// </summary>
        public string TextColor { get; set; }

        /// <summary>
        /// Button width in pixels
        /// Default: 200
        /// </summary>
        public int? Width { get; set; }

        /// <summary>
        /// Button height in pixels
        /// Default: 44
        /// </summary>
        public int? Height { get; set; }

        /// <summary>
        /// Border radius in pixels
        /// Default: 25
        /// </summary>
        public int? BorderRadius { get; set; }

        /// <summary>
        /// Font size in pixels
        /// Default: 16
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
        /// Border color for outlined buttons (hex format)
        /// If null, no border is applied
        /// </summary>
        public string BorderColor { get; set; }

        /// <summary>
        /// Border width in pixels (for outlined buttons)
        /// Default: 2
        /// </summary>
        public int? BorderWidth { get; set; }

        /// <summary>
        /// Padding around the button (CSS format, e.g., "20px 0")
        /// Default: 20px 0
        /// </summary>
        public string Padding { get; set; }

        /// <summary>
        /// Creates a default primary button
        /// </summary>
        public static EmailButtonModel CreatePrimary(string url, string text)
        {
            return new EmailButtonModel
            {
                Url = url,
                Text = text,
                BackgroundColor = "#007bff",
                TextColor = "#ffffff",
                Width = 200,
                Height = 44,
                BorderRadius = 25
            };
        }

        /// <summary>
        /// Creates a default outlined button
        /// </summary>
        public static EmailButtonModel CreateOutlined(string url, string text, string color = "#007bff")
        {
            return new EmailButtonModel
            {
                Url = url,
                Text = text,
                BackgroundColor = "#ffffff",
                TextColor = color,
                BorderColor = color,
                BorderWidth = 2,
                Width = 200,
                Height = 44,
                BorderRadius = 20
            };
        }

        /// <summary>
        /// Creates a pill-shaped button
        /// </summary>
        public static EmailButtonModel CreatePill(string url, string text, string backgroundColor = "#28a745")
        {
            return new EmailButtonModel
            {
                Url = url,
                Text = text,
                BackgroundColor = backgroundColor,
                TextColor = "#ffffff",
                Width = 220,
                Height = 50,
                BorderRadius = 50 // Full pill shape
            };
        }

        /// <summary>
        /// Creates a subtle rounded button
        /// </summary>
        public static EmailButtonModel CreateSubtle(string url, string text, string backgroundColor = "#6c757d")
        {
            return new EmailButtonModel
            {
                Url = url,
                Text = text,
                BackgroundColor = backgroundColor,
                TextColor = "#ffffff",
                Width = 200,
                Height = 44,
                BorderRadius = 8 // Subtle radius
            };
        }
    }
}
