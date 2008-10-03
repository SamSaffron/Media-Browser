using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.MediaCenter.Hosting;
using SamSoft.VideoBrowser.LibraryManagement;

namespace SamSoft.VideoBrowser
{
    public class MyHistoryOrientedPageSession : HistoryOrientedPageSession
    {

        private Application myApp;

        public Application Application
        {
            get { return myApp; }
            set { myApp = value; }
        }



        protected override void LoadPage(object target, string source, IDictionary<string, object> sourceData, IDictionary<string, object> uiProperties, bool navigateForward)
        {
            this.Application.NavigatingForward = navigateForward;
            if (!navigateForward)
            {
                if (uiProperties.ContainsKey("FolderItems"))
                {
                    this.Application.FolderItems = uiProperties["FolderItems"] as FolderItemListMCE;
                }
            }
            base.LoadPage(target, source, sourceData, uiProperties, navigateForward);
        }
    }
}
