using System;
using System.Collections.Generic;
using System.Text;
using MediaBrowser.Library.Providers;
using MediaBrowser.Library;
using MediaBrowser.Library.Sources;
using MediaBrowser.LibraryManagement;
using System.Text.RegularExpressions;
using NUnit.Framework;
using Microsoft.MediaCenter.UI;
using System.Reflection;
using System.IO;

namespace MediaBrowserTest
{
    // extension class for testing
    public static class MyExtension
    {

        internal static ItemSource GetChild(this ItemSource source, int index)
        {

            foreach (var item in source.ChildSources)
            {
                if (item.RawName.Contains(".svn"))
                {
                    continue;
                }
                if (index == 0)
                {
                    return item;
                }
                index--;
            }
            return null;
        } 
    }

    [TestFixture]
    public class TestTVDBMetadata
    {
        [SetUp]
        public void Setup()
        {
            typeof(Application).GetMethod("RegisterUIThread",BindingFlags.NonPublic | BindingFlags.Static).Invoke(null, null);
        }

        [Test]
        public void TestEpisodeNumberFromFile()
        {
            Assert.AreEqual("02", Helper.EpisodeNumberFromFile(@"c:\somefolder\Season 1\1x02 BFOD.avi"));
            Assert.AreEqual("02", Helper.EpisodeNumberFromFile(@"c:\somefolder\1x02 BFOD.avi"));
            Assert.AreEqual("02", Helper.EpisodeNumberFromFile(@"c:\somefolder\South.Park.s01e02 BFOD.avi"));
            Assert.AreEqual("02", Helper.EpisodeNumberFromFile(@"c:\somefolder\South.Park.s01.e02 BFOD.avi"));
            Assert.AreEqual("05", Helper.EpisodeNumberFromFile(@"c:\somefolder\South.Park.01x05 BFOD.avi"));
            Assert.AreEqual("02", Helper.EpisodeNumberFromFile(@"c:\seriesname\season1\South.Park.s01e02 BFOD.avi"));
            Assert.AreEqual("02", Helper.EpisodeNumberFromFile(@"c:\seriesname\season1\South.Park.S01E02 BFOD.avi"));
            Assert.AreEqual("05", Helper.EpisodeNumberFromFile(@"c:\seriesname\season 1\South.Park.01x05 BFOD.avi"));
            Assert.AreEqual("2", Helper.EpisodeNumberFromFile(@"c:\someseries\Season 1\2 - 22 Balloon.avi"));
            Assert.AreEqual("2", Helper.EpisodeNumberFromFile(@"c:\someseries\Saison 1\2 - 22 Balloon.avi"));
            Assert.AreEqual("2", Helper.EpisodeNumberFromFile(@"c:\someseries\Temporada 1\2 - 22 Balloon.avi"));
            Assert.AreEqual("2", Helper.EpisodeNumberFromFile(@"c:\someseries\Sæson 1\2 - 22 Balloon.avi"));
            Assert.AreEqual("03", Helper.EpisodeNumberFromFile(@"c:\someseries\Season1\103 BFOD.avi"));
            Assert.AreEqual("01", Helper.EpisodeNumberFromFile(@"c:\Flight.of.the.Conchords.S01E01.WS.PDTV.XviD-LOL.avi"));
            Assert.AreEqual("02", Helper.EpisodeNumberFromFile(@"c:\Flight.of.the.Conchords.S01E02.PDTV.XviD-NoTV.avi"));
            Assert.AreEqual("03", Helper.EpisodeNumberFromFile(@"c:\Flight.of.the.Conchords.S01E03.PDTV.XviD-NoTV.avi"));
            Assert.AreEqual("04", Helper.EpisodeNumberFromFile(@"c:\Flight.of.the.Conchords.S01E04.PDTV.XviD-NoTV.avi"));
            Assert.AreEqual("05", Helper.EpisodeNumberFromFile(@"c:\Flight.of.the.Conchords.S01E05.PDTV.XviD-NoTV.avi"));
            Assert.AreEqual("06", Helper.EpisodeNumberFromFile(@"c:\Flight.of.the.Conchords.S01E06.PDTV.XviD-NoTV.avi"));
            Assert.AreEqual("07", Helper.EpisodeNumberFromFile(@"c:\Flight.of.the.Conchords.S01E07.PDTV.XviD-NoTV.avi"));
            Assert.AreEqual("08", Helper.EpisodeNumberFromFile(@"c:\Flight.of.the.Conchords.S01E08.PDTV.XviD-NoTV.avi"));
            Assert.AreEqual("09", Helper.EpisodeNumberFromFile(@"c:\Flight.of.the.Conchords.S01E09.PDTV.XviD-NoTV.avi"));
            Assert.AreEqual("10", Helper.EpisodeNumberFromFile(@"c:\Flight.of.the.Conchords.S01E10.PDTV.XviD-NoTV.avi"));
            Assert.AreEqual("11", Helper.EpisodeNumberFromFile(@"c:\Flight.of.the.Conchords.S01E11.PDTV.XviD-NoTV.avi"));
            Assert.AreEqual("12", Helper.EpisodeNumberFromFile(@"c:\Flight.of.the.Conchords.S01E12.PDTV.XviD-NoTV.avi"));


            // Test network share
            Assert.AreEqual("11", Helper.EpisodeNumberFromFile(@"\\10.0.0.4\videos\TV\Mister TV\Season 12\Mister.Tv.S12E11.NONSE.avi"));
        }

        [Test]
        public void TestSeasonFromEpisodeName()
        {
            Assert.AreEqual("12", Helper.SeasonNumberFromEpisodeFile(@"c:\videos\TV\South Park\Season 12\South.Park.S12E11.OTHERSTUFF.avi"));
        }

        [Test]
        public void TestSourceNavigation()
        {
            var path = Path.GetFullPath(@"..\..\..\TestMediaBrowser\SampleMedia\TV");
            var source = new FileSystemSource(@"..\..\..\TestMediaBrowser\SampleMedia\TV");

            var item = source.GetChild(0); 
            Assert.AreEqual(ItemType.Series, item.ItemType);

            item = item.GetChild(0); 
            Assert.AreEqual(ItemType.Season, item.ItemType);

            item = item.GetChild(0);
            Assert.AreEqual(ItemType.Episode, item.ItemType);
        }


        [Test]
        public void TestStandardFile()
        {
            var provider = new TvDbProvider();
            var store = new MediaMetadataStore(new UniqueName("TestTVDB"));
            Item item = new Item();

            var path = Path.GetFullPath(@"..\..\..\TestMediaBrowser\SampleMedia\TV");

            item.Assign(new FileSystemSource(path));
            item.EnsureChildrenLoaded(false);
            item.EnsureMetadataLoaded();

 //           var i2 = item.Children[0].Children[0].Children[0];

            Assert.IsTrue(provider.NeedsRefresh(item, ItemType.Episode));

            provider.Fetch(item, ItemType.Episode, store, false);
        }

        [Test]
        public void TestRemoveNameCommments()
        {
            Assert.AreEqual("Hello", Helper.RemoveCommentsFromName("Hello[Comment]"));
            Assert.AreEqual("Hello World.avi", Helper.RemoveCommentsFromName("[Comment]Hello[Comment] World[Comment].avi"));
        }
    }
}
