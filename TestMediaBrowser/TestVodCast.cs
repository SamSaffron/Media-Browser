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

namespace TestMediaBrowser {
    [TestFixture]
    public class TestVodCast {


        [Test]
        public void TestPodcastFetching() {
            VodCast vodcast = new VodCast();
            MockMediaLocation location = new MockMediaLocation("test.vodcast");
            location.Contents = "http://www.abc.net.au/atthemovies/vodcast_wmv.xml";
            vodcast.Assign(location, null, Guid.NewGuid());

            Assert.IsTrue(vodcast.Children.Count > 0);
        }
    }
}
