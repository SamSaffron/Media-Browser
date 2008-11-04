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



       
        
        enum ViewEnum
        {
            DetailsView = 0,
            PosterView,
            PosterViewWithLabels,
            ThumbView,
            ThumbViewWithLabels
        }

        public int ViewIndex
        {
            get
            {
                return FolderItemsMCE.folderItems.Prefs.ViewIndex; 
            }
            set
            {
                switch ((ViewEnum)value)
                {
                    case ViewEnum.DetailsView:
                        if (!Config.Instance.EnableDetailView)
                        {
                            ViewIndex = (int)ViewEnum.PosterView;
                            return;
                        }
                        break;
                    case ViewEnum.PosterView:
                        if (!Config.Instance.EnablePosterView)
                        {
                            ViewIndex = (int)ViewEnum.PosterViewWithLabels;
                            return;
                        }
                        break;
                    case ViewEnum.PosterViewWithLabels:
                        if (!Config.Instance.EnablePosterView2)
                        {
                            ViewIndex = (int)ViewEnum.ThumbView;
                            return;
                        }
                        break;

                    case ViewEnum.ThumbView:
                        if (!Config.Instance.EnableThumbView)
                        {
                            ViewIndex = (int)ViewEnum.ThumbViewWithLabels;
                            return;
                        }
                        break;

                    case ViewEnum.ThumbViewWithLabels:
                        if (!Config.Instance.EnableThumbView2)
                        {
                            ViewIndex = (int)ViewEnum.DetailsView;
                            return;
                        }
                        break;
                }

                FolderItemsMCE.folderItems.Prefs.ViewIndex = value;
                FolderItemsMCE.folderItems.Prefs.Save();
                FirePropertyChanged("ViewIndex");
            }
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
