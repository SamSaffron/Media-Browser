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

        bool IsMovie{ get; }
        bool IsFolder { get; }
        DateTime CreatedDate { get; }
        DateTime ModifiedDate { get; }
        int RunningTime { get; }
        string RunningTimeString { get; }
        float IMDBRating { get; }
        string IMDBRatingString { get; }
        string Filename { get; }

        string Description { get; }
        string Title1 { get; }
        string Title2 { get; }
        string Overview { get; } 
    }
}
