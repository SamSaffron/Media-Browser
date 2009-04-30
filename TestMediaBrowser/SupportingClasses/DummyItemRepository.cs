using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MediaBrowser.Library;
using MediaBrowser.Library.Entities;
using MediaBrowser.Library.Interfaces;

namespace TestMediaBrowser {
    public class DummyItemRepository : IItemRepository {

        public void SaveItem(BaseItem item) {
        }

        public BaseItem RetrieveItem(Guid name) {
            return null;
        }

        public void SaveChildren(Guid ownerName, IEnumerable<Guid> children) {
        }

        public IEnumerable<Guid> RetrieveChildren(Guid id) {
            return null;
        }

        public PlaybackStatus RetrievePlayState(Guid id) {
            return null;    
        }

        public DisplayPreferences RetrieveDisplayPreferences(Guid id) {
            return null;
        }

        public void SavePlayState(PlaybackStatus playState) {

        }

        public void SaveDisplayPreferences(DisplayPreferences prefs) {

        }

        public void CleanCache() {

        }

        public bool ClearEntireCache() {
            return false;
        }


        public IEnumerable<IMetadataProvider> RetrieveProviders(Guid guid) {
            return null;
        }

        public void SaveProviders(Guid guid, IEnumerable<IMetadataProvider> providers) {
        }


    }
}
