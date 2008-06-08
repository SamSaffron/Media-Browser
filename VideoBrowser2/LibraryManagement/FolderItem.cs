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
        }

        public FolderItem(string filename, bool isFolder)
            : this (filename, isFolder, 
                isFolder ? 
                    System.IO.Path.GetFileName(filename) : 
                    System.IO.Path.GetFileNameWithoutExtension(filename))
        {
        }


        public FolderItem(string filename, bool isFolder, string description)
        {
            this.filename = filename;
            this.isFolder = isFolder;
            this.Description = description;

        }

        #endregion 

        #region Statics
        private static MD5CryptoServiceProvider CryptoService = new MD5CryptoServiceProvider();
        static object syncObj = new object();
        static MethodInfo fromStreamMethodInfo = null;
        #endregion 

        #region Privates 

        VirtualFolder virtualFolder = null;
        Image image = null;
        private string path = null;
        bool thumbLoaded = false;
        bool metadataLoaded = false;
        DateTime createdDate = DateTime.MinValue;
        DateTime modifiedDate = DateTime.MinValue;
        List<IFolderItem> contents;
        string filename;
        bool isFolder;
        string thumbPath;
        TVShow _tvshow;
        DateTime thumbDate = DateTime.MinValue;
        Movie _movie = null;
        string title2;
        string overview;

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
                }
                virtualFolder = value;
            } 
        }
       
        public override Image MCMLThumb
        {
            get
            {
                if (image == null)
                {
                    image = Helper.GetMCMLThumb(ThumbPath, IsVideo);
                }
                return image;
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
                modifiedDate = di.LastWriteTime;
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

        public override List<string> Genres
        {
            get 
            {
                EnsureMetadataLoaded(); 
                if (this.Movie != null)
                {
                    return this.Movie.Genres;
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
                    return _tvshow.Overview; 
                }
                else if (_movie != null)
                {
                    return _movie.Description;
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
            try
            {

                string path = System.IO.Path.GetDirectoryName(filename);
                if (File.Exists(System.IO.Path.Combine(path, "series.xml")))
                {
                    return false;
                }

                int i = 0;
                foreach (string file in MovieSearch(filename))
                {
                    // exclude tv shows
                    if (file.ToLower().EndsWith("series.xml"))
                    {
                        return false;
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
                else
                {
                    return true;
                }
            }
            catch (Exception)
            {
                Trace.WriteLine(filename + " is a faulty directory!");
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
                Trace.WriteLine(filename);
                Trace.WriteLine(e.ToString());
            }

            //Trace.WriteLine(this.Displayname + " " + createdDate.ToString());
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
                {
                    LoadTVMetadata();
                }

                metadataLoaded = true;
            }
            catch (Exception e)
            {
                Trace.WriteLine(e.ToString()); 
                // Suppress any exceptions so the UI does not blow up
            }
        }

        private void LoadTVMetadata()
        {
            // tv shows can not be folders
            if (IsFolder) return;

            string metadataPath = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(filename), "metadata");
            string metadataFile = System.IO.Path.Combine(metadataPath, System.IO.Path.GetFileNameWithoutExtension(filename) + ".xml");

            if (System.IO.File.Exists(metadataFile))
            {
                _tvshow = new TVShow(metadataFile); 

                // only load if not already loaded (may be loaded cause filename.jpg exists)
                if (string.IsNullOrEmpty(thumbPath))
                {
                    thumbPath = _tvshow.ThumbPath;
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

        private static Image ImageFromStream(Stream stream)
        {
            if (fromStreamMethodInfo == null)
            {
                lock (syncObj)
                {
                    MethodInfo[] mis = typeof(Image).GetMethods(BindingFlags.Static | BindingFlags.NonPublic);

                    foreach (MethodInfo mi in mis)
                    {
                        ParameterInfo[] pis = mi.GetParameters();
                        if (mi.Name == "FromStream" && pis.Length == 2)
                        {
                            if (pis[0].ParameterType == typeof(String) &&
                                pis[1].ParameterType == typeof(Stream))
                            {
                                fromStreamMethodInfo = mi;
                            }
                        }
                    }
                }
            }

            return (Image)fromStreamMethodInfo.Invoke(null, new object[] { null, stream });
        }

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
