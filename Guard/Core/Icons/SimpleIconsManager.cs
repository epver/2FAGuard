﻿using System.Reflection;
using System.Text.Json;
using Wpf.Ui.Appearance;
using static Guard.Core.Icons.IconManager;

namespace Guard.Core.Icons
{
    public class SimpleIcon
    {
        /// <summary>
        /// Title
        /// </summary>
        public required string T { get; set; }

        /// <summary>
        /// The svg path
        /// </summary>
        public required string P { get; set; }

        /// <summary>
        /// The hex color
        /// </summary>
        public required string H { get; set; }
    }

    class SimpleIconsManager
    {
        private static SimpleIcon[]? icons;
        private static string? version;
        private static string? license;

        private class SimpleIconJSON
        {
            public required string Version { get; set; }
            public required SimpleIcon[] Icons { get; set; }
            public required string License { get; set; }
        }

        public static void LoadIcons()
        {
            using var stream =
                Assembly
                    .GetExecutingAssembly()
                    .GetManifestResourceStream("Guard.Assets.Icons.si.json")
                ?? throw new Exception("Can not find internal SimpleIcon JSON file");
            using var reader = new System.IO.StreamReader(stream);
            var json = reader.ReadToEnd();
            var parsedJson =
                JsonSerializer.Deserialize<SimpleIconJSON>(json)
                ?? throw new Exception("Can not parse internal SimpleIcon JSON file");
            version = parsedJson.Version;
            icons = parsedJson.Icons;
            license = parsedJson.License;
        }

        public static SimpleIcon[]? GetIcons()
        {
            return icons;
        }

        public static string? GetVersion()
        {
            return version;
        }

        public static string[] GetIconNames() =>
            icons?.Select(icon => icon.T).ToArray()
            ?? throw new InvalidOperationException("Icons not loaded");

        public static SimpleIcon? GetSimpleIcon(string name)
        {
            return icons?.FirstOrDefault(icon => icon.T == name);
        }

        public static TotpIcon? GetTotpIcon(string name)
        {
            SimpleIcon? simpleIcon = GetSimpleIcon(name);
            if (simpleIcon == null)
            {
                return null;
            }

            string hexColor = simpleIcon.H;
            if (hexColor == null)
            {
                bool isDarkMode = ApplicationThemeManager.GetAppTheme() == ApplicationTheme.Dark;
                if (isDarkMode)
                {
                    hexColor = "#ffffff";
                }
                else
                {
                    hexColor = "#313131";
                }
            }

            if (!hexColor.StartsWith('#'))
            {
                hexColor = $"#{hexColor}";
            }

            return new TotpIcon
            {
                Type = IconType.SimpleIcons,
                Svg =
                    $"<svg xmlns=\"http://www.w3.org/2000/svg\" viewBox=\"0 0 24 24\" fill=\"{hexColor}\"><path d=\"{simpleIcon.P}\"/></svg>",
                Name = name,
            };
        }

        public static string? GetLicense()
        {
            return license;
        }
    }
}
