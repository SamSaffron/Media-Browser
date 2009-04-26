using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using MediaBrowser.Library.Filesystem;
using MediaBrowser.LibraryManagement;
using MediaBrowser.Library.Entities;
using MediaBrowser.Library.EntityDiscovery;
using MediaBrowser.Library.Factories;

namespace MediaBrowser.Library
{
    class ItemFactory 
    {
        public static readonly ItemFactory Instance = new ItemFactory();

        private ItemFactory() {

        }

        public Item Create(BaseItem baseItem) {
            Item item;
            if (baseItem is Folder) {
                item = new FolderModel();
            } else {
                item = new Item();
            }
            item.Assign(baseItem);
            return item;
        }

    }
}
