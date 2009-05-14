using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using MediaBrowser.Library.Entities;
using MediaBrowser.Library;

namespace TestMediaBrowser {
    [TestFixture]
    public class TestPlaybackStatus {

        [Test]
        public void TestWatchedIsFiredCorrectly() {
            var stat = new PlaybackStatus();
            stat.WasPlayedChanged += new EventHandler<EventArgs>(stat_OnWatchedChanged);
            played = stat.WasPlayed;
            stat.PlayCount = 1;
            Assert.IsTrue(played);
            stat.PlayCount = 0;
            Assert.IsFalse(played);
        }

        [Test]
        public void TestPersistance() {
            var stat = new PlaybackStatus();
            stat.LastPlayed = DateTime.Now;
            stat.PlayCount = 99;
            stat.PlaylistPosition = 2;
            stat.PositionTicks = 1000;
            stat.Id = Guid.NewGuid();

            stat.Save();

            var stat2 = Kernel.Instance.ItemRepository.RetrievePlayState(stat.Id);
            Assert.AreEqual(stat.Id, stat2.Id);
            Assert.AreEqual(stat.LastPlayed, stat2.LastPlayed);
            Assert.AreEqual(stat.PlayCount, stat2.PlayCount);
            Assert.AreEqual(stat.PlaylistPosition, stat2.PlaylistPosition);
            Assert.AreEqual(stat.PositionTicks, stat2.PositionTicks);  
        }

        private bool played;
        void stat_OnWatchedChanged(object sender, EventArgs e) {
            played = (sender as PlaybackStatus).WasPlayed;
        }
        
    }
}
