using System.IO;

namespace Flowery.WinUI.Gallery
{
    /// <summary>
    /// Verifies the external font files behind the <c>NotoSansFamily</c> resource of GalleryResources.xaml.
    /// </summary>
    public static class GalleryFontAssets
    {
        private const string FontUriPrefix = "ms-appx:///Assets/Fonts/";
        private const string LicenseTitle = "SIL OPEN FONT LICENSE Version 1.1";
        private const double ProbeFontSize = 40;

        // Representative text for each script that the font family covers.
        private static readonly Dictionary<string, string> Samples = new(StringComparer.Ordinal)
        {
            ["Noto Sans"] = "Flowery Ag",
            ["Noto Sans SC"] = "简体中文",
            ["Noto Sans JP"] = "日本語のかな",
            ["Noto Sans KR"] = "한국어",
            ["Noto Sans Arabic"] = "العربية",
            ["Noto Sans Hebrew"] = "עברית",
        };

        /// <summary>
        /// Returns one message for each font of the family that is missing, lacks its license notice, or does not render.
        /// </summary>
        /// <param name="family">The font family resource whose entries reference files in Assets/Fonts.</param>
        /// <param name="host">A loaded panel that temporarily hosts the text probes.</param>
        public static IReadOnlyList<string> FindProblems(FontFamily family, Panel host)
        {
            var problems = new List<string>();
            var directory = System.IO.Path.Combine(AppContext.BaseDirectory, "Assets", "Fonts");
            var entries = family.Source.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
            if (entries.Length == 0)
                problems.Add("The font family has no entries.");

            foreach (var entry in entries)
            {
                var separator = entry.IndexOf('#');
                if (!entry.StartsWith(FontUriPrefix, StringComparison.Ordinal) || separator < 0)
                {
                    problems.Add($"The font family entry does not reference Assets/Fonts: {entry}");
                    continue;
                }

                var fileName = entry[FontUriPrefix.Length..separator];
                var familyName = entry[(separator + 1)..];
                if (!File.Exists(System.IO.Path.Combine(directory, fileName)))
                    problems.Add($"The font file is missing: {fileName}");

                // Each family file, for example NotoSansSC-Variable.ttf, has its notice in NotoSansSC-OFL.txt.
                var licenseName = fileName[..Math.Max(fileName.IndexOf('-'), 0)] + "-OFL.txt";
                var license = System.IO.Path.Combine(directory, licenseName);
                if (!File.Exists(license) || !File.ReadAllText(license).Contains(LicenseTitle, StringComparison.Ordinal))
                    problems.Add($"The license notice for {fileName} is missing: {licenseName}");

                if (!Samples.TryGetValue(familyName, out var sample))
                    problems.Add($"No sample text exists for the font family {familyName}.");
                else if (!Renders(entry, entry[..separator] + "#Flowery Missing Font", sample, host))
                    problems.Add($"{familyName} does not render '{sample}' from {fileName}.");
            }

            return problems;
        }

        // A family name that the file does not contain falls back to a system font.
        // Different text metrics show that the requested font was loaded.
        private static bool Renders(string source, string fallbackSource, string sample, Panel host)
        {
            var font = CreateProbe(source, sample);
            var fallback = CreateProbe(fallbackSource, sample);
            var panel = new StackPanel { Opacity = 0, IsHitTestVisible = false, Children = { font, fallback } };
            host.Children.Add(panel);
            try
            {
                panel.UpdateLayout();
                return font.ActualWidth > 0 && font.ActualHeight > 0 &&
                    (Math.Abs(font.ActualWidth - fallback.ActualWidth) > 0.5 || Math.Abs(font.ActualHeight - fallback.ActualHeight) > 0.5);
            }
            finally
            {
                host.Children.Remove(panel);
            }
        }

        private static TextBlock CreateProbe(string source, string sample) => new()
        {
            Text = sample,
            FontFamily = new FontFamily(source),
            FontSize = ProbeFontSize,
            HorizontalAlignment = HorizontalAlignment.Left,
        };
    }
}
