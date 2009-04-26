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

        [Test]
        public void TestFilesystemImageCaching() {

            var imagePath = Path.Combine(Environment.CurrentDirectory,@"..\..\SampleMedia\Images\image.png");
            imagePath = new FileInfo(imagePath).FullName;

            var image = LibraryImageFactory.Instance.GetImage(imagePath);

            Assert.IsNotNull(image);

            // this makes sure images are not double/cached for the local drive ...
            Assert.AreEqual(imagePath, image.GetLocalImagePath());

        }
    }
}
