using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.MediaCenter.Hosting;
using MediaBrowser.Library;

namespace MediaBrowser
{
    public class MyHistoryOrientedPageSession : HistoryOrientedPageSession
    {

        private Application myApp;

        public Application Application
        {
            get { return myApp; }
            set { myApp = value; }
        }

        List<string> breadcrumbs = new List<string>();

        protected override void LoadPage(object target, string source, IDictionary<string, object> sourceData, IDictionary<string, object> uiProperties, bool navigateForward)
        {
            this.Application.NavigatingForward = navigateForward;
            if (navigateForward)
            {
                if (breadcrumbs.Count == 0) {
                    breadcrumbs.Add(Config.Instance.InitialBreadcrumbName);
                }
                else if ((uiProperties != null) && (uiProperties.ContainsKey("Item")))
                {
                    Item itm = (Item)uiProperties["Item"];
                    breadcrumbs.Add(itm.Name);
                } 
                else if ((uiProperties != null) && (uiProperties.ContainsKey("Folder"))) {
                    FolderModel folder = (FolderModel)uiProperties["Folder"];
                    breadcrumbs.Add(folder.Name);
                }
                else
                    breadcrumbs.Add("");
                
            }
            else if (breadcrumbs.Count > 0)
                breadcrumbs.RemoveAt(breadcrumbs.Count - 1);
            
            base.LoadPage(target, source, sourceData, uiProperties, navigateForward);
        }

        public string Breadcrumbs
        {
            get
            {
                int max = Config.Instance.BreadcrumbCountLimit;
                if (breadcrumbs.Count < max)
                    max = breadcrumbs.Count;
                if (max == 0)
                    return "";
                return string.Join(" | ", breadcrumbs.ToArray(), breadcrumbs.Count - max, max);
            }
        }
    }
}
