using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using TestMediaBrowser.SupportingClasses;
using MediaBrowser.Library.Factories;
using MediaBrowser.Library.Extensions;
using MediaBrowser.Library.Filesystem;
using MediaBrowser.Library;

namespace TestMediaBrowser {
    [TestFixture]
    public class TestResolvers {

        [Test]
        public void ShouldNotResolve() {
           
            
            var root = new MockFolderMediaLocation(); 
            var location = new MockFolderMediaLocation();
            location.Parent = root;
            location.Path = @"c:\A series\Season 08\metadata";

            Assert.IsFalse(location.IsSeriesFolder()); 

            Assert.IsNull(Kernel.Instance.GetItem(location));
        }

        public void WeShouldNotBeResolvingTheRecycleBin() {

            var root = new MockFolderMediaLocation();
            var location = new MockFolderMediaLocation();
            location.Parent = root;
            location.Path = @"c:\$ReCycle.bin";
            Assert.IsNull(Kernel.Instance.GetItem(location));
        }
    }
}
