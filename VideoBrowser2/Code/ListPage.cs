using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.MediaCenter.UI;
using SamSoft.VideoBrowser.LibraryManagement;
using System.IO;

namespace SamSoft.VideoBrowser
{
    public class ListPage : ModelItem 
    {
        public ListPage(FolderItemListMCE items, string path)
        {
            FolderItemsMCE = items;
            this.PagePath = path;
            items.folderItems.OnChanged += new FolderItemListModifiedDelegate(folderItems_OnChanged);
           
        }

        void folderItems_OnChanged()
        {
            
        }
        public FolderItemListMCE FolderItemsMCE { get; set; }
        public string PagePath { get; set; }
        public string BreadCrumbs
        {
            get 
            {
                try
                {
                    return FolderItemsMCE.Breadcrumb;
                }
                catch
                {
                    return "BANG";
                }
            }
        }   

        Image banner = null;
        bool bannerLoaded = false;
        public Image Banner
        {
            get 
            { 
                if (bannerLoaded)
                    return banner;
                else
                {
                    banner = Helper.GetMCMLBanner(GetBannerPath(this.PagePath,0)); 
                    bannerLoaded = true;
                    return banner;
                }
            }
        }

        public bool HasBanner
        {
            get { return (Banner != null); }
        }

        private string GetBannerPath(string startPath, int recursion)
        {
            if ((startPath == null) || (recursion > 2))
                return null;
            string f = Path.Combine(startPath, "banner.jpg");
            if (File.Exists(f))
                return f;
            else
                return GetBannerPath(Path.GetDirectoryName(startPath), recursion + 1);
        }
       
        public FolderItemListPrefs FolderPrefs
        {
            get { return FolderItemsMCE.folderItems.Prefs; }
        }
        
        public float ThumbAspectRatio
        {
            get
            {
                return FolderItemsMCE.folderItems.ThumbAspectRatio;
            }
        }

        
     }
}
