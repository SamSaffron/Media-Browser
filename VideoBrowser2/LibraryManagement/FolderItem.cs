using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Security.Cryptography;
using System.Diagnostics;
using Microsoft.MediaCenter.UI;
using System.Xml;
using SamSoft.VideoBrowser.Util;
using System.Reflection;

namespace SamSoft.VideoBrowser.LibraryManagement
{
    public class FolderItem : BaseFolderItem
    {
        #region Constructors

        public FolderItem()
        {
            // we need this so we can construct these puppies for mcml params 
            thumbPath = "";
            bannerPath = "";
        }

        public FolderItem(string filename, bool isFolder, bool useBanners)
            : this (filename, isFolder, 
                isFolder ? 
                    System.IO.Path.GetFileName(filename) :
                    System.IO.Path.GetFileNameWithoutExtension(filename), useBanners)
        {
        }

        public static string GetSortableDescription(string description)
        {
            string sortableDescription = description.ToLower();
            foreach (string search in Config.Instance.SortRemoveCharactersArray)
            {
                sortableDescription = sortableDescription.Replace(search.ToLower(), string.Empty);
            }
            foreach (string search in Config.Instance.SortReplaceCharactersArray)
            {
                sortableDescription = sortableDescription.Replace(search.ToLower(), " ");
            }
            foreach (string search in Config.Instance.SortReplaceWordsArray)
            {
                string searchLower = search.ToLower();
                // Remove from beginning if a space follows
                if (sortableDescription.StartsWith(searchLower + " "))
                {
                    sortableDescription = sortableDescription.Remove(0, searchLower.Length + 1);
                }
                // Remove from middle if surrounded by spaces
                sortableDescription = sortableDescription.Replace(" " + searchLower + " ", " ");

                // Remove from end if followed by a space
                if (sortableDescription.EndsWith(" " + searchLower))
                {
                    sortableDescription = sortableDescription.Remove(sortableDescription.Length - (searchLower.Length + 1));
                }
            }
            //sortableDescription = sortableDescription.Trim();
            return sortableDescription;
        }

        public FolderItem(string filename, bool isFolder, string description, bool useBanners)
        {
            this.filename = filename;
            this.isFolder = isFolder;
			this.Description = description;
            this.UseBanners = useBanners;

			// Sanitize description (for sorting)

            sortableDescription = GetSortableDescription(this.Description);

        }

        #endregion 

        #region Statics
        private static MD5CryptoServiceProvider CryptoService = new MD5CryptoServiceProvider();
        static object syncObj = new object();
        public const string DUMMY_DIR = "{919BC682-F0E4-47ba-9E08-899858D5D2BB}";
        #endregion 

        #region Privates 

        VirtualFolder virtualFolder = null;
        private string path = null;
        bool thumbLoaded = false;
        bool bannerLoaded = false;
        bool metadataLoaded = false;
        DateTime createdDate = DateTime.MinValue;
        DateTime modifiedDate = DateTime.MinValue;
        List<IFolderItem> contents;
        string filename;
        bool isFolder;
        string thumbPath;
        string bannerPath;
        TVShow _tvshow;
        TVSeries _tvSeries;
        DateTime thumbDate = DateTime.MinValue;
        DateTime bannerDate = DateTime.MinValue;
        Movie _movie = null;
        string title2;
        string overview;
		string sortableDescription;
  

        #endregion 

        public VirtualFolder VirtualFolder {
            get
            {
                return virtualFolder; 
            }
            set 
            {
                if (value.ThumbPath != null)
                {
                   ThumbPath = value.ThumbPath;
                   BannerPath = value.ThumbPath;
                }
                virtualFolder = value;
            } 
        }

        public override List<string> Directors
        {
            get 
            {
                if (IsMovie)
                {
                    return this.Movie.Directors;
                }

                return new List<string>() ;
            }
        }

        public string Path 
        {
            get 
            {
                if (path == null)
                {
                    path = System.IO.Path.GetDirectoryName(filename); 
                }
                return path;
            }
            set
            {
                path = value;
            }
        }

        // used for genre navigation   
        public List<IFolderItem> Contents { 
            get 
            {
                return contents; 
            }
            set 
            {
                contents = value; 
            }
        }

        public override float IMDBRating
        {
            get
            {
                EnsureMetadataLoaded();
                if (IsMovie)
                {
                    return Movie.IMDBRating;
                }
                else if (_tvshow != null)
                    return _tvshow.IMDBRating;
                else
                {
                    return -1;
                }

            }
        }

        public override string IMDBRatingString
        {
            get
            {
                return IMDBRating.ToString();
            } 
        }

        public override int RunningTime
        {
            get
            { 
                EnsureMetadataLoaded();
                if (IsMovie && Movie.RunningTime > 0)
                {
                    return Movie.RunningTime;
                }
                else
                {
                    return 0; 
                }
            }
        }

        public override int ProductionYear
        {
            get
            {
                EnsureMetadataLoaded();
                if (IsMovie && Movie.ProductionYear > 0)
                {
                    return Movie.ProductionYear;
                }
                else
                {
                    return 0;
                }
            }
        }


        public override string RunningTimeString
        {
            get
            {
                EnsureMetadataLoaded(); 
                if (IsMovie && Movie.RunningTime > 0)
                {
                    return Movie.RunningTime.ToString() + " minutes";
                }
                else
                {
                    return string.Empty; 
                }
            }
        }

        public override DateTime CreatedDate
        {
            get 
            {
                if (createdDate == DateTime.MinValue)
                {
                    InitTimes();
                }

                return createdDate;
            }
           
        }



        public override DateTime ModifiedDate
        {
            get
            {
                LoadModifiedDate();
                return modifiedDate;
            }
        }

        public void LoadModifiedDate()
        {
            if (modifiedDate == DateTime.MinValue)
            {
                DirectoryInfo di = new DirectoryInfo(filename);
                try
                {
                    modifiedDate = di.LastWriteTime;
                }
                catch
                {
                    modifiedDate = DateTime.Now;
                }
            }
        }

        public override string Filename
        {
            get { return filename; }
        }

        public override bool IsFolder
        {
            get { return isFolder; }
        }

		public override string SortableDescription
		{
			get { return sortableDescription; }
		}
		
		public override string ThumbHash
        {
            get
            {
                string key = ThumbPath;
                key += ThumbDate.ToString();
                key = Helper.HashString(key) + System.IO.Path.GetExtension(ThumbPath);
                return key;
            } 
        }

        public override string BannerHash
        {
            get
            {
                string key = BannerPath;
                key += BannerDate.ToString();
                key = Helper.HashString(key) + System.IO.Path.GetExtension(BannerPath);
                return key;
            }
        }

        public override List<string> Genres
        {
            get 
            {
                EnsureMetadataLoaded(); 
                if (this.Movie != null)
                    return this.Movie.Genres;
                else if (this._tvSeries != null)
                    return this._tvSeries.Genres;
                // To match implementation in cached version 
                return new List<string>(); 
            }
        }

        public override List<string> Actors
        {
            get
            {
                EnsureMetadataLoaded();
                if (this.Movie != null)
                {
                    var actors = new List<string>();
                    foreach (var a in this.Movie.Actors)
                    {
                        actors.Add(a.Name);
                    }

                    return actors;
                }
                else if (this._tvSeries != null)
                {
                    if (_tvSeries.Actors != null)
                    {
                        return new List<string>(_tvSeries.Actors);
                    }
                }
                // To match implementation in cached version 
                return new List<string>();
            }
        }


        public override string GenresString
        {
            get
            {
                EnsureMetadataLoaded();
                if (IsMovie && Movie.Genres.Count > 0)
                {
                    string returnStr = string.Empty;
                    foreach (string g in Movie.Genres)
                    {
                        returnStr += g + ", ";
                    }
                    return returnStr;
                }
                else if ((_tvSeries != null) && (_tvSeries.Genres.Count > 0))
                {
                    return string.Join(", ", _tvSeries.Genres.ToArray());
                }
                else
                {
                    return string.Empty;
                }
            }
        }

        public long ThumbDate
        {
            get
            {
                if (thumbDate == DateTime.MinValue)
                {

                    try
                    {
                        DirectoryInfo di = new DirectoryInfo(ThumbPath);
                        thumbDate = di.LastWriteTime;
                    }
                    catch
                    {
                        thumbDate = DateTime.MaxValue;
                    }
                }
                return thumbDate.ToBinary();
            } 
        }

        public long BannerDate
        {
            get
            {
                if (bannerDate == DateTime.MinValue)
                {

                    try
                    {
                        DirectoryInfo di = new DirectoryInfo(BannerPath);
                        bannerDate = di.LastWriteTime;
                    }
                    catch
                    {
                        bannerDate = DateTime.MaxValue;
                    }
                }
                return bannerDate.ToBinary();
            }
        } 

        public override string ThumbPath 
        {
            set
            {
                if (!string.IsNullOrEmpty(value))
                {
                    thumbPath = value;
                    thumbLoaded = true;
                }
                else
                {
                    thumbLoaded = false;
                    thumbPath = "";
                }
            }
            get
            {
                if (filename == null)
                {
                    return null;
                } 

                if (!thumbLoaded)
                { 
                    var tp = Helper.GetThumb(filename,IsFolder);
                    thumbLoaded = true;
                    if (!string.IsNullOrEmpty(tp))
                    {
                        thumbPath = tp; 
                    }
                }

                if (thumbPath == null && !metadataLoaded)
                {
                    LoadMetadata();
                } 

                return thumbPath; 
            }
        }

        public override string BannerPath
        {
            set
            {
                if (!string.IsNullOrEmpty(value))
                {
                    bannerPath = value;
                    bannerLoaded = true;
                }
                else
                {
                    bannerLoaded = false;
                    bannerPath = "";
                }
            }
            get
            {
                if (filename == null)
                {
                    return null;
                }

                if (!bannerLoaded)
                {
                    var tp = Helper.GetBanner(filename, IsFolder);
                    bannerLoaded = true;
                    if (!string.IsNullOrEmpty(tp))
                    {
                        bannerPath = tp;
                    }
                }

                if (bannerPath == null && !metadataLoaded)
                {
                    LoadMetadata();
                }

                return bannerPath;
            }
        }

        public void SetOverview(string val)
        {
            overview = val;
        }

        public override string Overview
        {
         //   set
         //   {
         //       overview = value;
         //   }
            get
            {
                if (overview != null)
                {
                    return overview;
                }

                EnsureMetadataLoaded();
                if (_tvshow != null)
                {
                    overview = _tvshow.Overview; 
                }
                else if (_tvSeries != null)
                {
                    overview =  _tvSeries.Overview;
                }
                else if (_movie != null)
                {
                    overview = _movie.Description;
                }

                if (overview != null)
                {
                    return overview;
                }
                else
                {
                    return "";
                }
            }
        }

        public void SetTitle2(string val)
        {
            title2 = val;
        }

        public override string Title2
        {
          //  set 
          //  {
          //      title2 = value;
          //  }

            get 
            {
                if (title2 != null)
                {
                    return title2;
                }

                EnsureMetadataLoaded();
                if (_tvshow != null)
                {
                    return _tvshow.EpisodeName;
                }
                else
                {
                    return "";
                } 
            }
        }

        public override string Title1
        {
            get 
            {
                string rval = string.Empty;

                EnsureMetadataLoaded();
                if (_tvshow != null)
                {
                    return _tvshow.LongSeriesName;
                }
                else if (_tvSeries != null)
                {
                    return _tvSeries.SeriesName;
                }

                return rval;
            }
        }

        internal void EnsureMetadataLoaded()
        {
            if (!metadataLoaded)
            {
                LoadMetadata();
                InitTimes();
            }
        } 

        public override string ToString()
        {
            return Description;
        }

        /// <summary>
        /// Returns true if the file is a movie, or its a folder only containing 2 files 
        /// </summary>
        public override bool IsVideo
        {
            get
            {
                return !IsFolder || FolderContainsMovie(); 
            } 
        }

        public override bool IsMovie
        {
            get
            {
                return Movie != null; 
            }
        } 

        public string[] movieFileListing = null;
        public string[] movieFileListingWithVobs = null;

        public string[] GetMovieListWithVobs()
        {
            if (movieFileListingWithVobs == null)
            {
                List<string> movies = new List<string>();

                foreach (string file in MovieSearch(filename, true, true))
                {
                    if (Helper.IsVideo(file) || Helper.IsDvd(file))
                    {
                        movies.Add(file);
                    }
                }
                movieFileListingWithVobs = movies.ToArray();
                Array.Sort(movieFileListingWithVobs);
            }
            return movieFileListingWithVobs;
        }

        public string[] GetMovieList()
        {
            if (movieFileListing == null)
            {
                List<string> movies = new List<string>();

                foreach (string file in MovieSearch(filename))
                {
                    if (Helper.IsVideo(file))
                    {
                        movies.Add(file);
                    }
                }
                movieFileListing = movies.ToArray();
                Array.Sort(movieFileListing);
            }
            return movieFileListing;
        }

        public Guid Hash
        {
            get
            {
                StringBuilder sb = new StringBuilder();
                sb.Append(this.createdDate.ToString());
                sb.Append(this.IsFolder.ToString());
                sb.Append(this.IsVideo.ToString());
                sb.Append(this.ThumbPath == null ? "<null>" : this.thumbPath);
                sb.Append(this.BannerPath == null ? "<null>" : this.bannerPath);
                sb.Append(this.filename);
                sb.Append(this.Path);

                Byte[] originalBytes = ASCIIEncoding.Default.GetBytes(sb.ToString());
                Byte[] encodedBytes = CryptoService.ComputeHash(originalBytes);
                return new Guid(encodedBytes);
            } 
        }

        private bool FolderContainsMovie()
        {
            // TODO: Do we cache this? 

            if (filename == DUMMY_DIR)
            {
                return false;
            }

            try
            {

                string path = System.IO.Path.GetDirectoryName(filename);
                // path may be null for network shares (get directory name will not work for //10.0.0.2/dd
                if (path == null || File.Exists(System.IO.Path.Combine(path, "series.xml")))
                {
                    return false;
                }

                int i = 0;
                bool hasIso = false;
                foreach (string file in MovieSearch(filename))
                {
                    // exclude tv shows
                    if (file.ToLower().EndsWith("series.xml"))
                    {
                        return false;
                    }

                    // We gotta check for ISO files here since
                    // we cannot playlist them.
                    if (Helper.isIso(file))
                    {
                        hasIso = true;
                    }

                    i++;
                    if (i > 2)
                    {
                        return false;
                    }
                }

                if (i == 0)
                {
                    return ContainsDvd; 
                }

                // 2 playable videos is fine, but not OK with ISO files.
                else if (i == 2)
                {
                    return !hasIso && Config.Instance.EnableMoviePlaylists;
                }

                else
                {
                    return true;
                }
            }
            catch (Exception)
            {
                Trace.TraceInformation(filename + " is a faulty directory!");
                return false;
            }
        }

        public bool ContainsDvd
        {
            get
            {
                var nestedPath = System.IO.Path.Combine(filename,"VIDEO_TS"); 
                bool nestedPathExists = Directory.Exists(nestedPath); 

                // check either in the dir or in video_ts
                return CheckForDVD(filename) || (nestedPathExists && CheckForDVD(nestedPath)); 
            }
        }
  
        private void InitTimes()
        {
            try
            {
                // dont care about times for non files 
                if ((filename == null) || (filename==""))
                {
                    return;
                }

                DirectoryInfo di = new DirectoryInfo(filename);
                modifiedDate = di.LastWriteTime;
                if (!IsFolder || !IsVideo)
                {
                    createdDate = di.CreationTime;    
                }
                else
                {

                    foreach (var item in GetMovieListWithVobs())
                    {
                        di = new DirectoryInfo(item);
                        // with created we are looking for earliest
                        if (createdDate == DateTime.MinValue || di.CreationTime < createdDate)
                        {
                            createdDate = di.CreationTime;

                        }
                    }
                }
            }
            catch (Exception e)
            {
                Trace.TraceInformation(filename);
                Trace.TraceInformation(e.ToString());
            }

            //Trace.TraceInformation(this.Displayname + " " + createdDate.ToString());
        }

        private IEnumerable<string> MovieSearch(string directory)
        {
            return MovieSearch(directory, false, Config.Instance.EnableNestedMovieFolders); 
        }

        private IEnumerable<string> MovieSearch(string directory, bool includeVobs, bool recursive)
        {
          
            foreach (string file in Directory.GetFiles(directory))
            {
                if (Helper.IsVideo(file) || file.ToLower().EndsWith("series.xml"))
                {
                    yield return file;
                }
                if (includeVobs && Helper.IsDvd(file))
                {
                    yield return file; 
                } 
            }

            if (recursive)
            {
                foreach (string file in Directory.GetDirectories(directory))
                {
                    foreach (string movie in MovieSearch(file, includeVobs, recursive))
                    {
                        yield return movie;
                    }
                }
            }
            
        }

        public Movie Movie 
        {
            get
            {
                return _movie; 
            }
        }

        #region Metadata loading 

        private void LoadMetadata()
        {
            // chuck a sleep to see what happens when its slow 
          //  System.Threading.Thread.Sleep(100);

            if (filename == null || filename.Length == 0)
            {
                return;
            }

            try
            {
                if (!LoadMovieMetadata())
                    LoadTVMetadata();
                

                metadataLoaded = true;
            }
            catch (Exception e)
            {
                Trace.TraceInformation(e.ToString()); 
                // Suppress any exceptions so the UI does not blow up
            }
        }

        private void LoadSeriesMetaData()
        {
            string metadataPath = System.IO.Path.Combine(filename, "series.xml");
            if (File.Exists(metadataPath))
            {
                _tvSeries = new TVSeries(metadataPath);
            }
        }

        private void LoadTVMetadata()
        {
            // tv shows can not be folders
            if (IsFolder)
            {
                LoadSeriesMetaData();
                return;
            }

            string metadataPath = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(filename), "metadata");
            string metadataFile = System.IO.Path.Combine(metadataPath, System.IO.Path.GetFileNameWithoutExtension(filename) + ".xml");

            if (System.IO.File.Exists(metadataFile))
            {
                _tvshow = new TVShow(metadataFile); 

                // only load if not already loaded (may be loaded cause filename.jpg exists)
                if (string.IsNullOrEmpty(thumbPath))
                {
                    thumbPath = _tvshow.ThumbPath;
                    bannerPath = _tvshow.ThumbPath;
                }
            }
        }

        private bool LoadMovieMetadata()
        {
            bool rval = false; 

            if (isFolder)
            {
                string movieMetadata = System.IO.Path.Combine(filename, "mymovies.xml"); 
                
                if (System.IO.File.Exists(movieMetadata))
                {
                    _movie = new Movie(movieMetadata);
                    rval = true; 
                }
            }
            return rval;
        }

        #endregion 

        private static bool CheckForDVD(string directory)
        {
            try
            {
                foreach (string file in Directory.GetFiles(directory))
                {
                    if (Helper.IsDvd(file))
                    {
                        return true;
                    }
                }
            }
            catch (IOException)
            {
                // fall through 
            }
            return false;
        }


    }
}
