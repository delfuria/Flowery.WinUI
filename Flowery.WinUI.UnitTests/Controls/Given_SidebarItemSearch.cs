using Flowery.Controls;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Flowery.WinUI.UnitTests.Controls
{
    [TestClass]
    public class Given_SidebarItemSearch
    {
        private static SidebarItem CreateTagPickerItem() =>
            new() { Id = "tagpicker", Name = "Sidebar_TagPicker", DisplayName = "Tag-Auswahl", ControlName = "DaisyTagPicker" };

        [TestMethod]
        public void When_SearchingTranslatedOrControlName_Matches()
        {
            var item = CreateTagPickerItem();

            foreach (var search in new[] { "Tag-Auswahl", "tag-auswahl", "Auswahl", "DaisyTagPicker", "TagPicker", "tag picker" })
            {
                Assert.IsTrue(item.MatchesSearch(search), search);
            }
        }

        [TestMethod]
        public void When_SearchingUnrelatedText_DoesNotMatch()
        {
            var item = CreateTagPickerItem();

            foreach (var search in new[] { "Toggle", "---" })
            {
                Assert.IsFalse(item.MatchesSearch(search), search);
            }
        }

        [TestMethod]
        public void When_ControlNameIsMissing_MatchesTranslatedNameOnly()
        {
            var item = new SidebarItem { Id = "modal-radii", Name = "Sidebar_ModalRadii", DisplayName = "Modal Corner Radii" };

            Assert.IsTrue(item.MatchesSearch("corner radii"));
            Assert.IsFalse(item.MatchesSearch("DaisyModal"));
        }
    }
}
