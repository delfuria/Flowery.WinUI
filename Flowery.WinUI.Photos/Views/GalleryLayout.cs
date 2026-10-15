using Flowery.WinUI.Photos.Localization;
using Flowery.WinUI.Photos.Models;
using Flowery.WinUI.Photos.Services;

namespace Flowery.WinUI.Photos.Views
{
    /// <summary>
    /// A virtualized row in the gallery: either a date header or a justified row of photos.
    /// Height includes the spacing below the row so offsets can be computed exactly.
    /// </summary>
    public abstract class GalleryRow
    {
        public double Top { get; set; }
        public double Height { get; set; }
        public DateTime Date { get; init; }
    }

    public sealed class GalleryHeaderRow : GalleryRow
    {
        public required string Title { get; init; }
        public required List<PhotoItem> Items { get; init; }
    }

    public sealed class GalleryPhotoRow : GalleryRow
    {
        public required List<(PhotoItem Item, double Width)> Tiles { get; init; }
        public double TileHeight { get; init; }
    }

    /// <summary>
    /// Builds justified ("Google Photos style") rows: every full row fills the available width while
    /// keeping each photo's aspect ratio. The last row of a group keeps the target height.
    /// </summary>
    public static class GalleryLayout
    {
        public const double HeaderHeight = 52;

        public static List<GalleryRow> Build(
            IReadOnlyList<PhotoItem> items,
            double width,
            double targetHeight,
            double gap,
            PhotoGroupMode groupMode,
            bool squareTiles)
        {
            List<GalleryRow> rows = [];
            if (items.Count == 0 || width < 50)
                return rows;

            var top = 0.0;
            foreach (var group in Group(items, groupMode))
            {
                if (group.Title != null)
                {
                    rows.Add(new GalleryHeaderRow
                    {
                        Title = group.Title,
                        Items = group.Items,
                        Date = group.Date,
                        Top = top,
                        Height = HeaderHeight
                    });
                    top += HeaderHeight;
                }

                var groupRows = squareTiles
                    ? BuildSquare(group.Items, width, targetHeight, gap)
                    : BuildJustified(group.Items, width, targetHeight, gap);

                foreach (var row in groupRows)
                {
                    row.Top = top;
                    rows.Add(row);
                    top += row.Height;
                }
            }

            return rows;
        }

        private static List<GalleryPhotoRow> BuildJustified(List<PhotoItem> items, double width, double targetHeight, double gap)
        {
            List<GalleryPhotoRow> rows = [];
            List<PhotoItem> pending = [];
            var aspectSum = 0.0;

            foreach (var item in items)
            {
                pending.Add(item);
                aspectSum += item.AspectRatio;
                var rowWidth = aspectSum * targetHeight + gap * (pending.Count - 1);
                if (rowWidth >= width)
                {
                    var height = (width - gap * (pending.Count - 1)) / aspectSum;
                    rows.Add(CreateRow(pending, height, gap, width, stretch: true));
                    pending = [];
                    aspectSum = 0;
                }
            }

            if (pending.Count > 0)
            {
                rows.Add(CreateRow(pending, targetHeight, gap, width, stretch: false));
            }

            return rows;
        }

        private static GalleryPhotoRow CreateRow(List<PhotoItem> items, double height, double gap, double width, bool stretch)
        {
            height = Math.Max(40, Math.Round(height));
            List<(PhotoItem, double)> tiles = [];
            var used = 0.0;
            for (var i = 0; i < items.Count; i++)
            {
                var w = Math.Floor(items[i].AspectRatio * height);
                if (stretch && i == items.Count - 1)
                    w = Math.Max(20, width - used);
                tiles.Add((items[i], w));
                used += w + gap;
            }

            return new GalleryPhotoRow
            {
                Tiles = tiles,
                TileHeight = height,
                Height = height + gap,
                Date = items[0].SortDate
            };
        }

        private static List<GalleryPhotoRow> BuildSquare(List<PhotoItem> items, double width, double targetHeight, double gap)
        {
            var perRow = Math.Max(1, (int)Math.Floor((width + gap) / (targetHeight + gap)));
            var side = Math.Floor((width - gap * (perRow - 1)) / perRow);
            List<GalleryPhotoRow> rows = [];
            for (var i = 0; i < items.Count; i += perRow)
            {
                var chunk = items.Skip(i).Take(perRow).ToList();
                rows.Add(new GalleryPhotoRow
                {
                    Tiles = [.. chunk.Select(c => (c, side))],
                    TileHeight = side,
                    Height = side + gap,
                    Date = chunk[0].SortDate
                });
            }
            return rows;
        }

        private sealed record PhotoGroup(string? Title, DateTime Date, List<PhotoItem> Items);

        private static IEnumerable<PhotoGroup> Group(IReadOnlyList<PhotoItem> items, PhotoGroupMode mode)
        {
            if (mode == PhotoGroupMode.None)
            {
                yield return new PhotoGroup(null, items[0].SortDate, [.. items]);
                yield break;
            }

            var culture = PhotosLocalization.CurrentCulture;
            List<PhotoItem> current = [];
            DateTime? currentKey = null;

            foreach (var item in items)
            {
                var date = item.SortDate;
                var key = mode switch
                {
                    PhotoGroupMode.Day => date.Date,
                    PhotoGroupMode.Year => new DateTime(date.Year, 1, 1),
                    _ => new DateTime(date.Year, date.Month, 1)
                };

                if (currentKey != null && key != currentKey)
                {
                    yield return new PhotoGroup(FormatGroupTitle(currentKey.Value, mode, culture), currentKey.Value, current);
                    current = [];
                }
                currentKey = key;
                current.Add(item);
            }

            if (current.Count > 0 && currentKey != null)
                yield return new PhotoGroup(FormatGroupTitle(currentKey.Value, mode, culture), currentKey.Value, current);
        }

        public static string FormatGroupTitle(DateTime date, PhotoGroupMode mode, CultureInfo culture)
        {
            return mode switch
            {
                PhotoGroupMode.Day => FormatLongDate(date, culture),
                PhotoGroupMode.Year => date.Year.ToString(culture),
                _ => Capitalize(date.ToString("Y", culture), culture)
            };
        }

        /// <summary>
        /// Long date without the weekday (e.g. "October 3, 2026" / "3. Oktober 2026").
        /// </summary>
        public static string FormatLongDate(DateTime date, CultureInfo culture)
        {
            var pattern = culture.DateTimeFormat.LongDatePattern;
            pattern = pattern.Replace("dddd", string.Empty, StringComparison.Ordinal).Trim(' ', ',', '.', '،');
            if (pattern.StartsWith(',') || pattern.StartsWith('.'))
                pattern = pattern[1..].Trim();
            try
            {
                return Capitalize(date.ToString(pattern, culture), culture);
            }
            catch (FormatException)
            {
                return date.ToString("D", culture);
            }
        }

        private static string Capitalize(string text, CultureInfo culture) =>
            string.IsNullOrEmpty(text) ? text : char.ToUpper(text[0], culture) + text[1..];

        /// <summary>
        /// Finds the index of the row at the given vertical offset (binary search on Top).
        /// </summary>
        public static int FindRowAt(IReadOnlyList<GalleryRow> rows, double offset)
        {
            if (rows.Count == 0)
                return -1;
            int lo = 0, hi = rows.Count - 1;
            while (lo < hi)
            {
                var mid = (lo + hi + 1) / 2;
                if (rows[mid].Top <= offset)
                    lo = mid;
                else
                    hi = mid - 1;
            }
            return lo;
        }
    }
}
