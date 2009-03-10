using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Diagnostics;

namespace MediaBrowser.Library.Playables
{
    public class PlayableExternal : PlayableItem
    {
        private static object lck = new object();
        private static Dictionary<MediaType, ConfigData.ExternalPlayer> configuredPlayers = null;
        private string path;
        public PlayableExternal(string path)
        {
            this.path = path;
        }

        public override void Prepare(bool resume)
        {
            
        }

        public override string Filename
        {
            get { return path; }
        }

        protected override void BeforeResume()
        {
            
        }

        protected override void PlayInternal( bool resume)
        {
            MediaType type  = MediaTypeResolver.DetermineType(this.path);
            ConfigData.ExternalPlayer p = configuredPlayers[type];
            string args = string.Format(p.Args, this.path);
            Process.Start(p.Command, args);
            MarkWatched();
        }

        public static bool CanPlay(string path)
        {
            if (RunningOnExtender)
                return false;
            if (configuredPlayers==null)
                lock(lck)
                    if (configuredPlayers==null)
                        LoadConfig();
            MediaType type = MediaTypeResolver.DetermineType(path);
            if (configuredPlayers.ContainsKey(type))
                return true;
            else
                return false;
        }

        private static void LoadConfig()
        {
 	        configuredPlayers = new Dictionary<MediaType,ConfigData.ExternalPlayer>();
            if (Config.Instance.ExternalPlayers!=null)
                foreach(var x in Config.Instance.ExternalPlayers)
                    configuredPlayers[x.MediaType] = x;
        }

        
    }
}
