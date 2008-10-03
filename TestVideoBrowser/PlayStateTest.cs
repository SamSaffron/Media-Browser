using System;
using System.Collections.Generic;
using System.Text;
using NUnit.Framework;
using SamSoft.VideoBrowser.LibraryManagement;

namespace TestVideoBrowser
{
    [TestFixture]
    class PlayStateTest
    {
        
        public void ShouldStoreDataCorrectly()
        {
            var fi = new MockFolderItem(); 
            var ps = new PlayState(fi);
            ps.Position = new TimeSpan(100);
            ps = new PlayState(fi);
            Assert.AreEqual(new TimeSpan(100),ps.Position); 
        }
    }
}
