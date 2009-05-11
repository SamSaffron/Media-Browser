using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using MediaBrowser.Library.Providers;
using MediaBrowser.Library.Metadata;

namespace TestMediaBrowser {
    [TestFixture]
    public class TestMetadataProvider {


        [Test]
        public void TestImageFromMediaLocationProviderIsFirst() {
            Assert.AreEqual(typeof(VirtualFolderProvider), MetadataProviderHelper.ProviderTypes[0]);
            Assert.AreEqual(typeof(ImageFromMediaLocationProvider), MetadataProviderHelper.ProviderTypes[1]);
        }
    }
}
