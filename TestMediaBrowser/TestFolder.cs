using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using TestMediaBrowser.SupportingClasses;
using MediaBrowser.Library;
using MediaBrowser.Library.Factories;
using MediaBrowser.Library.Entities;
using MediaBrowser.Library.Filesystem;
using MediaBrowser.Library.Interfaces;

namespace TestMediaBrowser {
    [TestFixture]
    public class TestFolder {

        [Test]
        public void TestChildCaching() {
            var rootLocation = MockFolderMediaLocation.CreateMockLocation(
            @"
|Root
 movie3.avi
 movie4.avi
 movie5.avi
");
            var rootFolder = Kernel.Instance.GetItem<MediaBrowser.Library.Entities.Folder>(rootLocation) ;
            rootFolder.Id = Guid.NewGuid();

            Assert.AreEqual(3, rootFolder.Children.Count());

            var cached = new MediaBrowser.Library.Entities.Folder();
            cached.Id = rootFolder.Id;

            Assert.AreEqual(3, cached.Children.Count());
        }

        [Test]
        public void TestChildValidation() {
            var rootLocation = MockFolderMediaLocation.CreateMockLocation(
            @"
|Root
 movie3.avi
 movie4.avi
 movie5.avi
");
            var rootLocationNew = MockFolderMediaLocation.CreateMockLocation(
            @"
|Root
 movie3.avi
 movie4.avi
 movie5.avi
 movie6.avi
");

            var rootFolder = Kernel.Instance.GetItem(rootLocation) as MediaBrowser.Library.Entities.Folder;
            rootFolder.Id = Guid.NewGuid();

            Assert.AreEqual(3, rootFolder.Children.Count());

            var oldFactory = Kernel.Instance.MediaLocationFactory;

            Kernel.Instance.MediaLocationFactory = new MockMediaLocationFactory(rootLocationNew);
            rootFolder.ValidateChildren();

            Assert.AreEqual(4, rootFolder.Children.Count());

            Kernel.Instance.MediaLocationFactory = oldFactory;


        }
    }
}
