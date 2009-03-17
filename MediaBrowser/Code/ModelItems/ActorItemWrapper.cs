using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.MediaCenter.UI;
using MediaBrowser.Library.Sources;
using MediaBrowser.Code.ModelItems;

namespace MediaBrowser.Library
{
    public class ActorItemWrapper : BaseModelItem
    {
        public Actor Actor { get; private set; }
        private Item parent;
        private Item item = null;

        public ActorItemWrapper(Actor actor, Item parentItem)
        {
            this.Actor = actor;
            this.parent = parentItem;
        }

        public Item Item
        {
            get
            {
                if (item==null)
                    lock(this)
                        if (item == null)
                        {
                            FilterSource<Actor> source = new FilterSource<Actor>(parent.UnsortedChildren, this.Actor,
                                                            delegate(Item itm, Actor actor) { return itm.Metadata.Actors.Find(a => a.Name == actor.Name) != null; },
                                                            ItemType.Actor,
                                                            delegate(Actor actor) { return actor.Name; });
                            item = source.ConstructItem();
                        }
                return item;
            }
        }
    }
}
