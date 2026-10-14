using Flowery.Skia;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SkiaSharp;

namespace Flowery.WinUI.RuntimeTests.Controls
{
    [TestClass]
    public class Given_SkiaImageProcessing
    {
        [TestMethod]
        public void When_SaturationIsZero_AllChannelsContainTheSameLuminance()
        {
            var red = Process(new SKColor(255, 0, 0), 0);
            Assert.AreEqual(red.Red, red.Green, "Grayscale must have equal red and green channels.");
            Assert.AreEqual(red.Green, red.Blue, "Grayscale must have equal green and blue channels.");
            Assert.AreEqual(54d, red.Red, 1d, "Red must use the documented BT.709 luminance weight.");

            var gray = Process(new SKColor(100, 100, 100), 0);
            Assert.AreEqual((byte)100, gray.Red);
            Assert.AreEqual(gray.Red, gray.Green);
            Assert.AreEqual(gray.Green, gray.Blue);
        }

        [TestMethod]
        public void When_PurpleIsPartiallyDesaturated_ItsHueAndNeutralTintArePreserved()
        {
            var source = new SKColor(120, 80, 160);
            var partial = Process(source, 0.8f);
            Assert.AreEqual(115d, partial.Red, 1d);
            Assert.AreEqual(83d, partial.Green, 1d);
            Assert.AreEqual(147d, partial.Blue, 1d);
            Assert.AreEqual((byte)255, partial.Alpha);
            Assert.AreEqual(source, Process(source, 1));
        }

        private static SKColor Process(SKColor color, float saturation)
        {
            using var source = new SKBitmap(new SKImageInfo(8, 8));
            source.Erase(color);
            var bytes = SkiaImageProcessor.ApplyBlurAndSaturation(source, 0, saturation);
            Assert.IsNotNull(bytes);
            using var result = SKBitmap.Decode(bytes);
            Assert.IsNotNull(result);
            return result.GetPixel(4, 4);
        }
    }
}
