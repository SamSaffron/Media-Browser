using System;
using System.Collections.Generic;
using System.Text;
using MediaBrowser.Library.Providers;
using MediaBrowser.Library;
using MediaBrowser.LibraryManagement;
using System.Text.RegularExpressions;
using NUnit.Framework;
using Microsoft.MediaCenter.UI;
using System.Reflection;
using System.IO;
using MediaBrowser.Library.Providers.TVDB;
using MediaBrowser.Library.Entities;
using TestMediaBrowser.SupportingClasses;
using MediaBrowser.Library.Factories;
using TestMediaBrowser;

namespace MediaBrowserTest { 
    

    [TestFixture]
    public class TestTVDBMetadata
    {

        [Test]
        public void TestEpisodeNumberFromFile()
        {
            Assert.AreEqual("02", TVUtils.EpisodeNumberFromFile(@"c:\somefolder\Season 1\1x02 BFOD.avi"));
            Assert.AreEqual("02", TVUtils.EpisodeNumberFromFile(@"c:\somefolder\1x02 BFOD.avi"));
            Assert.AreEqual("02", TVUtils.EpisodeNumberFromFile(@"c:\somefolder\South.Park.s01e02 BFOD.avi"));
            Assert.AreEqual("02", TVUtils.EpisodeNumberFromFile(@"c:\somefolder\South.Park.s01.e02 BFOD.avi"));
            Assert.AreEqual("05", TVUtils.EpisodeNumberFromFile(@"c:\somefolder\South.Park.01x05 BFOD.avi"));
            Assert.AreEqual("02", TVUtils.EpisodeNumberFromFile(@"c:\seriesname\season1\South.Park.s01e02 BFOD.avi"));
            Assert.AreEqual("02", TVUtils.EpisodeNumberFromFile(@"c:\seriesname\season1\South.Park.S01E02 BFOD.avi"));
            Assert.AreEqual("05", TVUtils.EpisodeNumberFromFile(@"c:\seriesname\season 1\South.Park.01x05 BFOD.avi"));
            Assert.AreEqual("2", TVUtils.EpisodeNumberFromFile(@"c:\someseries\Season 1\2 - 22 Balloon.avi"));
            Assert.AreEqual("2", TVUtils.EpisodeNumberFromFile(@"c:\someseries\Saison 1\2 - 22 Balloon.avi"));
            Assert.AreEqual("2", TVUtils.EpisodeNumberFromFile(@"c:\someseries\Temporada 1\2 - 22 Balloon.avi"));
            Assert.AreEqual("2", TVUtils.EpisodeNumberFromFile(@"c:\someseries\Sæson 1\2 - 22 Balloon.avi"));
            Assert.AreEqual("03", TVUtils.EpisodeNumberFromFile(@"c:\someseries\Season1\103 BFOD.avi"));
            Assert.AreEqual("01", TVUtils.EpisodeNumberFromFile(@"c:\Flight.of.the.Conchords.S01E01....dfdfdf..avi"));
            Assert.AreEqual("02", TVUtils.EpisodeNumberFromFile(@"c:\Flight.of.the.Conchords.S01E02.avi"));
            Assert.AreEqual("03", TVUtils.EpisodeNumberFromFile(@"c:\Flight.of.the.Conchords.S01E03.sdad.avi"));
            Assert.AreEqual("04", TVUtils.EpisodeNumberFromFile(@"c:\Flight.of.the.Conchords.S01E04.dfg.avi"));
            Assert.AreEqual("05", TVUtils.EpisodeNumberFromFile(@"c:\Flight.of.the.Conchords.S01E05.dfgfd.avi"));
            Assert.AreEqual("06", TVUtils.EpisodeNumberFromFile(@"c:\Flight.of.the.Conchords.S01E06.dsfdsf 119223 sdfd.avi"));
            Assert.AreEqual("07", TVUtils.EpisodeNumberFromFile(@"c:\Flight.of.the.Conchords.S01E07.sfddsfsf2.avi"));
            Assert.AreEqual("08", TVUtils.EpisodeNumberFromFile(@"c:\Flight.of.the.Conchords.S01E08.dsfsdf.avi"));
            Assert.AreEqual("09", TVUtils.EpisodeNumberFromFile(@"c:\Flight.of.the.Conchords.S01E09.sdfsdfdff.avi"));
            Assert.AreEqual("10", TVUtils.EpisodeNumberFromFile(@"c:\Flight.of.the.Conchords.S01E10.sdfsdfs.avi"));
            Assert.AreEqual("11", TVUtils.EpisodeNumberFromFile(@"c:\Flight.of.the.Conchords.S01E11.sfdsdf.avi"));
            Assert.AreEqual("12", TVUtils.EpisodeNumberFromFile(@"c:\Flight.of.the.Conchords.S01E12.sdfsdf.avi"));

            // Test network share
            Assert.AreEqual("11", TVUtils.EpisodeNumberFromFile(@"\\10.0.0.4\videos\TV\Mister TV\Season 12\Mister.Tv.S12E11.NONSE.avi"));
        }

        [Test]
        public void TestEpisodeNumberThatContainsSeasonNumber() {
            Assert.AreEqual("05", TVUtils.EpisodeNumberFromFile(@"c:\weeds\season 1\Weeds - 105 - 100 things happend.avi"));
        } 

        [Test]
        public void TestSeasonFromEpisodeName()
        {
            Assert.AreEqual("12", TVUtils.SeasonNumberFromEpisodeFile(@"c:\videos\TV\South Park\Season 12\South.Park.S12E11.OTHERSTUFF.avi"));
        }

        
        [Test]
        public void TestRemoveNameCommments()
        {
            Assert.AreEqual("Hello", Helper.RemoveCommentsFromName("Hello[Comment]"));
            Assert.AreEqual("Hello World.avi", Helper.RemoveCommentsFromName("[Comment]Hello[Comment] World[Comment].avi"));
        }

        // The test below can be a bit slow cause the will talk to the internet 

        [Test]
        public void IntegrationTest() {

            var oldRepository = ItemCache.Instance;
            
            try {
                // Swap out item caching 
                ItemCache.Instance = new DummyItemRepository();

                var folder = MockFolderMediaLocation.CreateMockLocation(
    @"
|Weeds
 |Season 1
  01.avi
  02.avi
  03.avi
");

                var root = Kernel.Instance.GetItem<Folder>(folder);

                Series series = (Series)root;
                Season season = (Season)root.Children[0];
                Episode episode = (Episode)season.Children[0];

                series.RefreshMetadata();

                Assert.AreEqual("74845", series.TVDBSeriesId);

                season.RefreshMetadata();
                episode.RefreshMetadata();

                Assert.AreEqual("1", episode.EpisodeNumber);
            } finally {
                ItemCache.Instance = oldRepository;
            }
        }

   
        [Test]
        public void TestFetchingSeriesData() {
            Series series = new Series();
            series.Name = "Weeds";

            var provider = new RemoteSeriesProvider();
            provider.Item = series;

            Assert.IsTrue(provider.NeedsRefresh());

            provider.Fetch();

            Assert.AreEqual("74845", series.TVDBSeriesId);

        }

        [Test]
        public void TestDownloadingEpisodeData() {
            Series series = new Series();
            series.Name = "Weeds";
            series.TVDBSeriesId = "74845";

            
            Season season = new Season();
            season.SeasonNumber = "4";
            season.Parent = series;

            Episode episode = new Episode();
            episode.Parent = season;
            episode.Path = @"c:\shows\weeds\season 4\hello 402 - bla bla.avi";

            RemoteEpisodeProvider provider = new RemoteEpisodeProvider();
            provider.Item = episode;

            Assert.IsTrue(provider.NeedsRefresh());

            provider.Fetch();

            Assert.AreEqual("2", episode.EpisodeNumber);
        }
    }
}
