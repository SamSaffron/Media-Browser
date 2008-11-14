using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.MediaCenter.UI;

namespace SamSoft.VideoBrowser.LibraryManagement
{
    /// <summary>
    /// Contains the basic folder details required for poster and details view 
    /// </summary>
    public interface IFolderItem
    {
        Image MCMLThumb { get; }
        Image PosterViewThumb { get; }
        /// <summary>
        /// A globally unique ID for the file (based off Guid)
        /// </summary>
        string Key { get; }

        bool IsMovie{ get; }
        bool IsFolder { get; }
        DateTime CreatedDate { get; }
        DateTime ModifiedDate { get; }
        int RunningTime { get; }
        int ProductionYear { get; }
        List<string> Genres{ get; }
        List<string> Actors { get; }
        string RunningTimeString { get; }
        string GenresString { get; }
        float IMDBRating { get; }
        string IMDBRatingString { get; }
        string Filename { get; }
        string ThumbPath { get; set; }

        string Description { get; }
        string Title1 { get; }
        string Title2 { get; }
        string Overview { get; }
		string SortableDescription { get; }
        string LastWatched { get; }
        bool HaveWatched { get; }
    }
}
