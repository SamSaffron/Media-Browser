using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MediaBrowser.Library.Entities;
using System.ServiceModel.Syndication;
using System.Xml;
using System.Net;
using MediaBrowser.Library.Extensions;
using TestMediaBrowser.SupportingClasses;
using NUnit.Framework;
using System.Threading;
using MediaBrowser.Library.Network;


namespace TestMediaBrowser {
    [TestFixture]
    public class TestVodCast {


        [Test]
        public void TestPodcastFetching() {
            VodCast vodcast = new VodCast();
            MockMediaLocation location = new MockMediaLocation("test.vodcast");
            location.Contents = "url:http://www.abc.net.au/atthemovies/vodcast_wmv.xml";
            vodcast.Assign(location, null, Guid.NewGuid());

            Assert.IsTrue(vodcast.Children.Count > 0);
        }

        [Test]
        public void TestPodcastInitialization() {
            
            MockMediaLocation location = new MockMediaLocation("test.vodcast");
            location.Contents = "url : http://somewhere.com\n";
            location.Contents += "download_policy : FirstPlay\n";
            location.Contents += "files_to_retain : 10\n";

            VodCast vodcast = new VodCast();
            vodcast.Assign(location, null, Guid.NewGuid());

            Assert.AreEqual(vodcast.Url, "http://somewhere.com");
            Assert.AreEqual(vodcast.DownloadPolicy, DownloadPolicy.FirstPlay);
            Assert.AreEqual(vodcast.FilesToRetain, 10);
        } 
    }
}
