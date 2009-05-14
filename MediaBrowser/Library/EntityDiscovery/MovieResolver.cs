using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MediaBrowser.Library.Entities;
using MediaBrowser.Library.Filesystem;
using MediaBrowser.Library.Factories;
using MediaBrowser.LibraryManagement;
using MediaBrowser.Library.Extensions;

namespace MediaBrowser.Library.EntityDiscovery {
    public class MovieResolver : EntityResolver {

        int maxVideosPerMovie;
        bool searchForVideosRecursively; 

        public MovieResolver(int maxVideosPerMovie, bool searchForVideosRecursively) {
            this.maxVideosPerMovie = maxVideosPerMovie;
            this.searchForVideosRecursively = searchForVideosRecursively;
        }

        public override void ResolveEntity(IMediaLocation location, 
            out BaseItemFactory factory, 
            out IEnumerable<InitializationParameter> setup) {

            factory = null;
            setup = null;
            bool isMovie = false;
            MediaType mediaType = MediaType.Unknown;
            List<IMediaLocation> volumes = null;

            var folder = location as IFolderMediaLocation;
            if (folder != null && !folder.ContainsChild(FolderResolver.IGNORE_FOLDER)) {
                DetectFolderWhichIsMovie(folder, out isMovie, out mediaType, out volumes);

            } else {
                if (location.IsIso()) {
                    isMovie = true;
                    mediaType = MediaType.ISO;
                } else {
                    isMovie = location.IsVideo();
                }

            }

            if (isMovie) {
                factory = BaseItemFactory<Movie>.Instance;
                setup = new List<InitializationParameter>() {
                    new MediaTypeInitializationParameter() {MediaType = mediaType}
                };

                if (volumes != null && volumes.Count > 0) {
                    (setup as List<InitializationParameter>).Add(new MovieVolumeInitializationParameter() { Volumes = volumes });
                }
            }
            
        }

        private void DetectFolderWhichIsMovie(IFolderMediaLocation folder, out bool isMovie, out MediaType mediaType, out List<IMediaLocation> volumes) {
            int isoCount = 0;
            var childFolders = new List<IFolderMediaLocation>();
            isMovie = false;
            mediaType = MediaType.Unknown;

            volumes = new List<IMediaLocation>();

            foreach (var child in folder.Children) {
                var pathUpper = child.Path.ToUpper();

                if (pathUpper.EndsWith("VIDEO_TS") || pathUpper.EndsWith(".VOB")) {
                    isMovie = true;
                    mediaType = MediaType.DVD;
                    break;
                }

                if (pathUpper.EndsWith("HVDVD_TS")) {
                    isMovie = true;
                    mediaType = MediaType.HDDVD;
                    break;
                }

                if (pathUpper.EndsWith("BDMV")) {
                    isMovie = true;
                    mediaType = MediaType.BluRay;
                    break;
                }

                if (child.IsIso()) {
                    mediaType = MediaType.ISO;
                    isoCount++;
                    if (isoCount > 1) {
                        break;
                    }
                }

                if (pathUpper.EndsWith("NOAUTOPLAYLIST")) {
                    break;
                }

                var childFolder = child as IFolderMediaLocation;
                if (childFolder != null) {
                    childFolders.Add(childFolder);
                }

                if (child.IsVideo()) {
                    volumes.Add(child);
                    if (volumes.Count > maxVideosPerMovie || isoCount > 0) {
                        break;
                    }
                }
            }

            if (searchForVideosRecursively && isoCount == 0) {

                int currentCount = volumes.Count;

                volumes.AddRange(childFolders
                    .Select(child => ChildVideos(child))
                    .SelectMany(x => x)
                    .Take((maxVideosPerMovie - currentCount) + 1));
            }

            if (volumes.Count > 0 && isoCount == 0) {
                if (volumes.Count <= maxVideosPerMovie) {
                    isMovie = true;
                }
            }

            if (volumes.Count == 0 && isoCount == 1) {
                isMovie = true;
            } 

            return;
        }

        private IEnumerable<IMediaLocation> ChildVideos(IFolderMediaLocation location) {

            if (location.ContainsChild(FolderResolver.IGNORE_FOLDER)) yield break;

            foreach (var child in location.Children) {
                if (child.IsVideo()) { 
                    yield return child;
                }
                var folder = child as IFolderMediaLocation;
                if (folder != null) {
                    foreach (var grandChild in ChildVideos(folder)) {
                        yield return grandChild;  
                    } 
                }
            }
            
        }
    }
}
