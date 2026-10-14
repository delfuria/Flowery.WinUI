using System;
using System.Collections.Generic;
using System.Linq;
using Flowery.Helpers;
using Flowery.Theming;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;

namespace Flowery.Controls
{
    /// <summary>Resolves theme lists without changing the source entries or applying a theme.</summary>
    internal static class ThemeListResolver
    {
        internal static List<DaisyThemeInfo> AvailableThemes(IEnumerable<DaisyThemeInfo> themes) =>
            [.. Order(themes, static info => info.Name, static info => info.Name, DaisyThemeManager.ThemeOverrides, true)];

        /// <summary>
        /// Applies global and local overrides, exclusions and the preferred group to a dropdown list.
        /// Returns new entries; the preferred group is followed by a separator on the first other entry.
        /// </summary>
        internal static List<ThemePreviewInfo> Resolve(IEnumerable<ThemePreviewInfo> source,
            IReadOnlyDictionary<string, DaisyThemeManager.ThemeEntry> localOverrides)
        {
            var overrides = new Dictionary<string, DaisyThemeManager.ThemeEntry>(DaisyThemeManager.ThemeOverrides, StringComparer.OrdinalIgnoreCase);
            foreach (var entry in localOverrides)
                overrides[entry.Key] = entry.Value;

            var entries = new Dictionary<string, ThemePreviewInfo>(StringComparer.OrdinalIgnoreCase);
            foreach (var item in source)
                entries[item.Name] = item;
            foreach (var entry in overrides.Values)
            {
                if (DaisyThemeManager.IsThemeVisible(entry.Name))
                    entries[entry.Name] = CreatePreview(entry, entries.TryGetValue(entry.Name, out var existing) ? existing : null);
            }

            List<ThemePreviewInfo> ordered = [.. Order(entries.Values, static item => item.Name, static item => item.DisplayName, overrides, overrides.Count > 0)];
            var boundary = ordered.FindIndex(item => !IsPreferred(item.Name, overrides));
            var result = new List<ThemePreviewInfo>(ordered.Count);
            for (var index = 0; index < ordered.Count; index++)
                result.Add(ordered[index].WithSeparator(boundary > 0 && index == boundary));
            return result;
        }

        private static IEnumerable<T> Order<T>(IEnumerable<T> source, Func<T, string> name, Func<T, string> displayName,
            IReadOnlyDictionary<string, DaisyThemeManager.ThemeEntry> overrides, bool alphabetical)
        {
            var ordered = source.Where(item => DaisyThemeManager.IsThemeVisible(name(item)))
                .OrderByDescending(item => IsPreferred(name(item), overrides));
            return alphabetical
                ? ordered.ThenBy(item => overrides.TryGetValue(name(item), out var entry) ? entry.DisplayName : displayName(item), StringComparer.CurrentCultureIgnoreCase)
                    .ThenBy(name, StringComparer.OrdinalIgnoreCase)
                : ordered.ThenBy(item => IsPreferred(name(item), overrides) ? displayName(item) : string.Empty,
                    StringComparer.CurrentCultureIgnoreCase);
        }

        private static bool IsPreferred(string name, IReadOnlyDictionary<string, DaisyThemeManager.ThemeEntry> overrides) =>
            overrides.TryGetValue(name, out var entry)
                ? entry.Preferred
                : DaisyThemeManager.IsThemePreferred(name);

        /// <summary>
        /// Creates the entry for an override. Reuses the brushes of an existing list entry, otherwise
        /// builds them from the registered factory or the product palette.
        /// </summary>
        private static ThemePreviewInfo CreatePreview(DaisyThemeManager.ThemeEntry entry, ThemePreviewInfo? existing)
        {
            if (existing != null)
                return existing.WithDisplayName(entry.DisplayName);

            var registered = DaisyThemeManager.GetThemeInfo(entry.Name);
            var product = entry.ProductPalette;
            var preview = new ThemePreviewInfo
            {
                Name = entry.Name,
                DisplayNameOverride = entry.DisplayName,
                IsDark = registered?.IsDark ?? (product != null && FloweryColorHelpers.IsDark(product.Background))
            };

            ResourceDictionary? palette = null;
            if (registered != null)
            {
                DaisyThemeManager.TryCreatePalette(entry.Name, out palette);
            }
            else if (product != null)
            {
                var precompiled = ProductPalettes.Get(entry.Name);
                palette = precompiled != null
                    ? DaisyPaletteFactory.Create(precompiled)
                    : ProductPaletteFactory.CreateResourceDictionary(product);
            }

            if (palette != null)
                preview.ApplyPaletteBrushes(palette);
            return preview;
        }
    }
}
