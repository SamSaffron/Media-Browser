using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using TestMediaBrowser.SupportingClasses;
using MediaBrowser.Library;
using MediaBrowser.Library.Entities;
using MediaBrowser.Library.Factories;
using MediaBrowser;
using MediaBrowser.Util;

namespace TestMediaBrowser {
    [TestFixture] 
    public class TestLibrary {



        [Test]
        public void TestLibraryNavigation() {

            var oldRepository = ItemCache.Instance;
            ItemCache.Instance = new DummyItemRepository();

            var rootLocation = MockFolderMediaLocation.CreateMockLocation(
            @"
|Root
 |Doco
  |movie1
   a.avi
   b.avi
  |movie2
   a.avi
 movie3.avi
 movie4.avi
 movie5.avi
");
            var rootFolder = BaseItemFactory.Instance.Create(rootLocation) as MediaBrowser.Library.Entities.Folder;

            Assert.IsNotNull(rootFolder);
            Assert.AreEqual(4, rootFolder.Children.Count());

            foreach (var item in rootFolder.Children.Skip(1)) {
                Assert.AreEqual(typeof(Movie), item.GetType());
            }

            Assert.AreEqual("movie3", rootFolder.Children.ElementAt(1).Name);
            ItemCache.Instance = oldRepository;

        }

        [Ignore("Only used for performance testing!")]
        [Test]
        public void TestScanPerformance() {
            var root = BaseItemFactory.Instance.Create(MediaLocationFactory.Instance.Create(
                Config.Instance.InitialFolder)) as Folder;

            foreach (var item in root.RecursiveChildren) {
                Console.WriteLine(item.Path);
            }

            foreach (var item in root.RecursiveChildren) {
                using (new Profiler("Refresh Metadata: " + item.Path))
                {
                    item.RefreshMetadata();
                }
            }
        }
    }
}
