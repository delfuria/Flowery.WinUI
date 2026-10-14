using System;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;

namespace Flowery.WinUI.Gallery.Examples
{
    public static class GalleryAssetHelper
    {
        private static readonly ImageSource TileBlueSource = CreateImageSource("Assets/tile_blue.jpg");
        private static readonly ImageSource TileGreenSource = CreateImageSource("Assets/tile_green.jpg");
        private static readonly ImageSource TilePurpleSource = CreateImageSource("Assets/tile_purple.jpg");
        private static readonly ImageSource WallpaperBlueSource = CreateImageSource("Assets/wallpaper_blue.jpg");
        private static readonly ImageSource WallpaperGreenSource = CreateImageSource("Assets/wallpaper_green.jpg");
        private static readonly ImageSource WallpaperRedSource = CreateImageSource("Assets/wallpaper_red.jpg");
        private static readonly ImageSource WallpaperYellowSource = CreateImageSource("Assets/wallpaper_yellow.jpg");

        public static ImageSource TileBlue => TileBlueSource;
        public static ImageSource TileGreen => TileGreenSource;
        public static ImageSource TilePurple => TilePurpleSource;
        public static ImageSource WallpaperBlue => WallpaperBlueSource;
        public static ImageSource WallpaperGreen => WallpaperGreenSource;
        public static ImageSource WallpaperRed => WallpaperRedSource;
        public static ImageSource WallpaperYellow => WallpaperYellowSource;

        private static BitmapImage CreateImageSource(string assetPath)
        {
            var trimmed = assetPath.TrimStart('/');
            var uri = new Uri($"ms-appx:///{trimmed}", UriKind.Absolute);
            return new BitmapImage(uri);
        }
    }
}
