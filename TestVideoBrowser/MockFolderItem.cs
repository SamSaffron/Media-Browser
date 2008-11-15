using System;
using System.Collections.Generic;
using System.Text;
using SamSoft.VideoBrowser.LibraryManagement;
using Microsoft.MediaCenter.UI;

namespace TestVideoBrowser
{
    class MockFolderItem : IFolderItem
    {
        #region IFolderItem Members

        public Vector3 PosterZoom { get{throw new NotImplementedException();} }

        public Microsoft.MediaCenter.UI.Image MCMLSmallThumb
        {
            get { throw new NotImplementedException(); }
        }

        public Microsoft.MediaCenter.UI.Image MCMLThumb
        {
            get { throw new NotImplementedException(); }
        }

        public string Key
        {
            get { return "unique_key"; }
        }

        public bool IsMovie
        {
            get { throw new NotImplementedException(); }
        }

        public bool IsFolder
        {
            get { throw new NotImplementedException(); }
        }

        public DateTime CreatedDate
        {
            get { throw new NotImplementedException(); }
        }

        public DateTime ModifiedDate
        {
            get { throw new NotImplementedException(); }
        }

        public int RunningTime
        {
            get { throw new NotImplementedException(); }
        }

        public int ProductionYear
        {
            get { throw new NotImplementedException(); }
        }

        public List<string> Genres
        {
            get { throw new NotImplementedException(); }
        }

        public List<string> Actors
        {
            get { throw new NotImplementedException(); }
        }

        public string RunningTimeString
        {
            get { throw new NotImplementedException(); }
        }

        public string GenresString
        {
            get { throw new NotImplementedException(); }
        }

        public float IMDBRating
        {
            get { throw new NotImplementedException(); }
        }

        public string IMDBRatingString
        {
            get { throw new NotImplementedException(); }
        }

        public string Filename
        {
            get { throw new NotImplementedException(); }
        }

        public string ThumbPath
        {
            get
            {
                throw new NotImplementedException();
            }
            set
            {
                throw new NotImplementedException();
            }
        }

        public string Description
        {
            get { throw new NotImplementedException(); }
        }

        public string Title1
        {
            get { throw new NotImplementedException(); }
        }

        public string Title2
        {
            get { throw new NotImplementedException(); }
        }

        public string Overview
        {
            get { throw new NotImplementedException(); }
        }

        #endregion

        #region IFolderItem Members


        public string SortableDescription
        {
            get { throw new NotImplementedException(); }
        }

        public string LastWatched
        {
            get { throw new NotImplementedException(); }
        }

        public bool HaveWatched
        {
            get { throw new NotImplementedException(); }
        }

        #endregion
    }
}
