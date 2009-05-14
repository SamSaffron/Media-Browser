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
using System.Diagnostics;

namespace TestMediaBrowser {
    [TestFixture] 
    public class TestLibrary {

        IItemRepository oldRepository; 

        [SetUp]
        public void Setup() {
            oldRepository = Kernel.Instance.ItemRepository;
            Kernel.Instance.ItemRepository = new DummyItemRepository();
        }

        [TearDown]
        public void Teardown() {
            Kernel.Instance.ItemRepository = oldRepository;
        } 

        [Test]
        public void TestISOResolution () { 
             var rootLocation = MockFolderMediaLocation.CreateMockLocation(
            @"
|Root
 Rocky.iso
 Rambo.iso
");
             var rootFolder = Kernel.Instance.GetItem <MediaBrowser.Library.Entities.Folder>(rootLocation);

            Assert.AreEqual(2, rootFolder.Children.Count());
            Assert.AreEqual(MediaType.ISO, (rootFolder.Children[0] as Video).MediaType);
            Assert.AreEqual(MediaType.ISO, (rootFolder.Children[1] as Video).MediaType);
        } 


        [Test]
        public void TestLibraryNavigation() {

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
            var rootFolder = Kernel.Instance.GetItem<MediaBrowser.Library.Entities.Folder>(rootLocation);

            Assert.IsNotNull(rootFolder);
            Assert.AreEqual(4, rootFolder.Children.Count());

            foreach (var item in rootFolder.Children.Skip(1)) {
                Assert.AreEqual(typeof(Movie), item.GetType());
            }

            Assert.AreEqual("movie3", rootFolder.Children.ElementAt(1).Name);
        }

        [Ignore("Only used for performance testing!")]
        [Test]
        public void TestScanPerformance() {
            var root = Kernel.Instance.GetItem<Folder>(Config.Instance.InitialFolder);

            foreach (var item in root.RecursiveChildren) {
                Console.WriteLine(item.Path);
                if (item.Name.ToLower() == "metadata") {

                    item.Parent.ValidateChildren();

                  //  Console.WriteLine(item.Path);
                    Debugger.Break();
                    
                }
            }

            foreach (var item in root.RecursiveChildren) {
                if (!(item.Path.Contains("The Office (US)\\Season 5") || item.Path.EndsWith("The Office (US)"))) continue;
                using (new Profiler("Refresh Metadata: " + item.Path))
                {
                    item.RefreshMetadata();
                }
            }
        }
    }
}
