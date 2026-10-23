using System.Threading.Tasks;
using Flowery.WinUI.Gallery;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Flowery.WinUI.RuntimeTests.Controls;

[TestClass]
public class Given_GalleryFontAssets
{
    [TestMethod]
    public async Task When_GalleryFontsLoad_EveryScriptRendersAndEachFontHasItsLicense()
    {
        var host = new Grid();
        RuntimeTestHelpers.AttachToHost(host);
        await RuntimeTestHelpers.EnsureLoadedAsync(host);

        var family = (FontFamily)Application.Current.Resources["NotoSansFamily"];
        var problems = GalleryFontAssets.FindProblems(family, host);

        Assert.AreEqual(0, problems.Count, string.Join(" ", problems));
    }
}
