using System.Collections.Generic;
using Microsoft.MediaCenter.Hosting;
using Microsoft.MediaCenter;

namespace SamSoft.VideoBrowser
{
    public class MyAddIn : IAddInModule, IAddInEntryPoint
    {

        public void Initialize(Dictionary<string, object> appInfo, Dictionary<string, object> entryPointInfo)
        {
        }

        public void Uninitialize()
        {
        }

        public void Launch(AddInHost host)
        {
        //  uncomment to debug
        //  host.MediaCenterEnvironment.Dialog("debug", "debug", DialogButtons.Ok, 100, true); 
            Application app = new Application(new MyHistoryOrientedPageSession(), host);
            app.GoToMenu();

        }
    }
}