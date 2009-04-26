using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MediaBrowser.Library.Filesystem;
using MediaBrowser.LibraryManagement;
using MediaBrowser.Library.Providers.TVDB;

namespace MediaBrowser.Library.Extensions {
    static class IMediaLocationExtensions {

        public static bool IsVideo(this IMediaLocation location) {
            return Helper.IsVideo(location.Path);
        }

        public static bool IsSeriesFolder(this IMediaLocation location) { 
            IFolderMediaLocation folder = location as IFolderMediaLocation;
            if (folder != null) {

                if (TVUtils.IsSeasonFolder(folder.Path))
                    return false;

                int i = 0;

                foreach (IMediaLocation child in folder.Children) {
                    if (child is IFolderMediaLocation &&
                        TVUtils.IsSeasonFolder(child.Path))
                        return true; // we have found at least one season folder
                    else
                        i++;
                    if (i >= 3)
                        return false; // a folder with more than 3 non-season folders in will not be counted as a series
                }

                foreach (IMediaLocation child in folder.Children) {
                    if (!(child is IFolderMediaLocation)  &&
                        TVUtils.EpisodeNumberFromFile(child.Path, false) != null)
                        return true;
                }
            }
            return false;
        }

        public static bool IsVodcast(this IMediaLocation location) {
            return location.Path.ToLower().EndsWith(".vodcast");
        }
    }
}
