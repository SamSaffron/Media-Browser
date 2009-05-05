using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using MediaBrowser.Library.ImageManagement;
using System.IO;
using MediaBrowser.Library.Factories;

namespace TestMediaBrowser {

    [TestFixture]
    public class TestLibraryImage {

        string image1 = Path.Combine(Environment.CurrentDirectory, @"..\..\SampleMedia\Images\image.png");
        string image2 = Path.Combine(Environment.CurrentDirectory, @"..\..\SampleMedia\Images\image2.png");

        [Test]
        public void TestFilesystemImageCaching() {

            var imagePath = new FileInfo(image1).FullName;

            var image = LibraryImageFactory.Instance.GetImage(imagePath);

            Assert.IsNotNull(image);

            // this makes sure images are not double/cached for the local drive ...
            Assert.AreEqual(imagePath, image.GetLocalImagePath());
        }

        [Test]
        public void IfAFileChangesLibraryImageShouldPickItUp() {

            // note this test case fails if the machine has no d: drive
            // We do not perform image caching on the c drive so this would never fail

            string tempPath = "d:\\testimages";
            
            try {
                Directory.CreateDirectory(tempPath);

                string target = Path.Combine(tempPath, "image.png");
                File.Copy(image1, target);

                var image = LibraryImageFactory.Instance.GetImage(target);
                Assert.AreEqual(new FileInfo(image1).Length, new FileInfo(image.GetLocalImagePath()).Length);

                LibraryImageFactory.Instance.ClearCache();

                File.Copy(image2, target, true);
                image = LibraryImageFactory.Instance.GetImage(target);
                Assert.AreEqual(new FileInfo(image2).Length, new FileInfo(image.GetLocalImagePath()).Length);
            } finally {
                Directory.Delete(tempPath, true);
            }
        }
    }
}
