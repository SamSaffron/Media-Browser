using System;
using System.Text;
using System.Collections.Generic;

using NUnit.Framework;
using MediaBrowser;
using MediaBrowser.LibraryManagement;
using MediaBrowser.Library.Sources;
using MediaBrowser.Library;
using System.Reflection;

namespace TestMediaBrowser
{
    /// <summary>
    /// Summary description for TestItemSource
    /// </summary>
    [TestFixture]
    public class TestItemSource
    {
        public TestItemSource()
        {
            //
            // TODO: Add constructor logic here
            //
        }

        string intialFolderBackup = "";

        [SetUp]
        public void Setup()
        {
            typeof(Microsoft.MediaCenter.UI.Application).GetMethod("RegisterUIThread", BindingFlags.NonPublic | BindingFlags.Static).Invoke(null, null);
            intialFolderBackup = Config.Instance.InitialFolder;
        }

        [TearDown]
        public void Teardown()
        {
            Config.Instance.InitialFolder = intialFolderBackup;
        }

        [Test]
        public void TestItemSourceCorrectForMyVideos()
        {

            Config.Instance.InitialFolder = Helper.MY_VIDEOS;
            var source = new FileSystemSource(Helper.MyVideosPath);
            Assert.AreEqual(ItemType.Folder, source.ItemType); 
        }
    }
}
