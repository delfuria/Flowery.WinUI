using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Flowery.WinUI.Photos.Models;
using Microsoft.UI.Xaml.Media.Imaging;
using Windows.Storage;
using Windows.Storage.FileProperties;
using Windows.Storage.Streams;

namespace Flowery.WinUI.Photos.Services
{
    /// <summary>
    /// Loads downscaled thumbnails with bounded concurrency and a small LRU cache.
    /// Common formats are decoded directly (fast scaled JPEG decode); exotic formats use the shell thumbnail cache.
    /// Must be called from the UI thread.
    /// </summary>
    public static class ThumbnailLoader
    {
        private const int MaxCacheEntries = 700;
        private static readonly SemaphoreSlim Gate = new(6);
        private static readonly Dictionary<string, LinkedListNode<(string Key, BitmapImage Image)>> Cache = [];
        private static readonly LinkedList<(string Key, BitmapImage Image)> Lru = new();

        private static readonly HashSet<string> DirectDecode = new(StringComparer.OrdinalIgnoreCase)
        {
            "JPG", "JPEG", "JPE", "JFIF", "PNG", "GIF", "BMP", "WEBP", "ICO"
        };

        public static int Bucket(double height)
        {
            if (height <= 120) return 120;
            if (height <= 200) return 200;
            if (height <= 300) return 300;
            if (height <= 440) return 440;
            if (height <= 640) return 640;
            return 960;
        }

        public static BitmapImage? TryGetCached(PhotoItem item, double height)
        {
            var key = CreateKey(item, Bucket(height));
            if (!Cache.TryGetValue(key, out var node))
                return null;
            Lru.Remove(node);
            Lru.AddFirst(node);
            return node.Value.Image;
        }

        public static async Task<BitmapImage?> LoadAsync(PhotoItem item, double height, CancellationToken token)
        {
            var bucket = Bucket(height);
            var key = CreateKey(item, bucket);
            if (Cache.TryGetValue(key, out var cached))
            {
                Lru.Remove(cached);
                Lru.AddFirst(cached);
                return cached.Value.Image;
            }

            await Gate.WaitAsync(token).ConfigureAwait(true);
            try
            {
                token.ThrowIfCancellationRequested();
                var image = DirectDecode.Contains(item.Extension)
                    ? await DecodeDirectAsync(item.Path, bucket, token).ConfigureAwait(true)
                    : null;
                image ??= await DecodeShellAsync(item.Path, bucket).ConfigureAwait(true);
                if (image == null)
                    return null;

                var node = Lru.AddFirst((key, image));
                Cache[key] = node;
                while (Lru.Count > MaxCacheEntries && Lru.Last is { } last)
                {
                    Cache.Remove(last.Value.Key);
                    Lru.RemoveLast();
                }
                return image;
            }
            catch (OperationCanceledException)
            {
                return null;
            }
            catch
            {
                return null;
            }
            finally
            {
                Gate.Release();
            }
        }

        public static void Invalidate(PhotoItem item)
        {
            var prefix = item.Path + "|";
            foreach (var key in Cache.Keys.Where(k => k.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)).ToList())
            {
                Lru.Remove(Cache[key]);
                Cache.Remove(key);
            }
        }

        private static string CreateKey(PhotoItem item, int bucket) => $"{item.Path}|{item.Revision}|{bucket}";

        private static async Task<BitmapImage?> DecodeDirectAsync(string path, int bucket, CancellationToken token)
        {
            var bytes = await Task.Run(() => File.ReadAllBytesAsync(path, token), token).ConfigureAwait(true);
            token.ThrowIfCancellationRequested();

            var stream = new InMemoryRandomAccessStream();
            using (var writer = new DataWriter(stream.GetOutputStreamAt(0)))
            {
                writer.WriteBytes(bytes);
                await writer.StoreAsync();
                writer.DetachStream();
            }
            stream.Seek(0);

            var image = new BitmapImage
            {
                DecodePixelType = DecodePixelType.Logical,
                DecodePixelHeight = bucket
            };
            try
            {
                await image.SetSourceAsync(stream);
                return image;
            }
            catch
            {
                return null;
            }
        }

        private static async Task<BitmapImage?> DecodeShellAsync(string path, int bucket)
        {
            try
            {
                var file = await StorageFile.GetFileFromPathAsync(path);
                using var thumb = await file.GetThumbnailAsync(ThumbnailMode.SingleItem, (uint)Math.Max(bucket * 2, 256), ThumbnailOptions.ResizeThumbnail);
                if (thumb == null || thumb.Type == ThumbnailType.Icon)
                    return null;
                var image = new BitmapImage();
                await image.SetSourceAsync(thumb);
                return image;
            }
            catch
            {
                return null;
            }
        }
    }
}
