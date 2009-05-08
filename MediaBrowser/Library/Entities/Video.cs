using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MediaBrowser.Library.Persistance;
using MediaInfoLib;
using MediaBrowser.Library.Filesystem;
using MediaBrowser.Library.EntityDiscovery;
using MediaBrowser.Library.Entities.Attributes;
using MediaBrowser.LibraryManagement;
using MediaBrowser.Library.Extensions;
using System.IO;

namespace MediaBrowser.Library.Entities {
    public class Video : Media {

        protected IMediaLocation location;

        public IMediaLocation MediaLocation {
            get {
                if (location == null) {
                    location = (IMediaLocation)MediaLocationFactory.Create(Path);
                }
                return location;
            }
        }


        [NotSourcedFromProvider]
        [Persist]
        public MediaType MediaType { get; set; }

        [Persist]
        public int? RunningTime { get; set; }

        [Persist]
        public MediaInfoData MediaInfo { get; set; }

        public override void Assign(IMediaLocation location, IEnumerable<InitializationParameter> parameters, Guid id) {
            base.Assign(location, parameters, id);

            if (parameters != null) {
                foreach (var parameter in parameters) {
                    var mediaTypeParam = parameter as MediaTypeInitializationParameter; 
                    if (mediaTypeParam != null ) {
                        MediaType = mediaTypeParam.MediaType;
                    }
                }
            }
        }

        public override bool AssignFromItem(BaseItem item) {
            bool changed = this.MediaType != ((Video)item).MediaType;
            this.MediaType = ((Video)item).MediaType;
            return changed | base.AssignFromItem(item);
        }

        private PlaybackStatus playbackStatus; 
        public PlaybackStatus PlaybackStatus {
            get {

                if (playbackStatus != null) return playbackStatus;

                playbackStatus = ItemCache.Instance.RetrievePlayState(this.Id);
                if (playbackStatus == null) {
                    playbackStatus = PlaybackStatusFactory.Instance.Create(Id); // initialise an empty version that items can bind to
                    if (DateCreated <= Config.Instance.AssumeWatchedBefore)
                        playbackStatus.PlayCount = 1;
                    playbackStatus.Save();
                }
                return playbackStatus;
            }
        }


        public virtual IEnumerable<string> VideoFiles {
            get {
                if (!ContainsRippedMedia && MediaLocation is IFolderMediaLocation) {
                    foreach (var path in GetChildVideos((IFolderMediaLocation)MediaLocation)) {
                        yield return path;
                    }
                } else {
                    yield return Path;
                }
            }
        }

        /// <summary>
        /// Returns true if the Video is from ripped media (DVD , BluRay , HDDVD or ISO)
        /// </summary>
        public bool ContainsRippedMedia {
            get {
                return MediaType == MediaType.BluRay ||
                    MediaType == MediaType.DVD ||
                    MediaType == MediaType.ISO ||
                    MediaType == MediaType.HDDVD;
            }
        }

        IEnumerable<string> GetChildVideos(IFolderMediaLocation location) {
            if (location.Path.EndsWith("$RECYCLE.BIN")) yield break;

            foreach (var child in location.Children)
	        {
                if (child.IsVideo()) yield return child.Path;
                else if (child is IFolderMediaLocation) {
                    foreach (var grandChild in GetChildVideos(child as IFolderMediaLocation)) {
                        yield return grandChild;
                    }
                }
	        }
        }

    }
}
