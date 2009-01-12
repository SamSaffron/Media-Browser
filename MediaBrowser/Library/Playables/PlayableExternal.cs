using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Diagnostics;

namespace MediaBrowser.Library.Playables
{
    public class PlayableExternal : PlayableItem
    {
        public enum MediaTypes { Unknown, Avi, Mkv, DVD, BlueRay, HDDVD }

        private static object lck = new object();
        private static Dictionary<MediaTypes, ConfigData.ExternalPlayer> configuredPlayers = null;
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

        public override void Play(PlayState playstate, bool resume)
        {
            MediaTypes type  = DetermineType(this.path);
            ConfigData.ExternalPlayer p = configuredPlayers[type];
            string args = string.Format(p.Args, this.path);
            Process.Start(p.Command, args);
        }

        public static bool CanPlay(string path)
        {
            if (configuredPlayers==null)
                lock(lck)
                    if (configuredPlayers==null)
                        LoadConfig();
            MediaTypes type = DetermineType(path);
            if (configuredPlayers.ContainsKey(type))
                return true;
            else
                return false;
        }

        private static void LoadConfig()
        {
 	        configuredPlayers = new Dictionary<MediaTypes,ConfigData.ExternalPlayer>();
            if (Config.Instance.ExternalPlayers!=null)
                foreach(var x in Config.Instance.ExternalPlayers)
                    configuredPlayers[x.MediaType] = x;
        }

        private static MediaTypes DetermineType(string path)
        {
            path = path.ToLower();
            if (path.Contains("video_ts"))
                return MediaTypes.DVD;
            if (path.EndsWith(".avi"))
                return MediaTypes.Avi;
            if (path.EndsWith(".mkv"))
                return MediaTypes.Mkv;
            if (path.Contains("bdmv"))
                return MediaTypes.BlueRay;
            if (path.Contains("hvdvd_ts"))
                return MediaTypes.HDDVD;
            if (Directory.Exists(Path.Combine(path, "VIDEO_TS")))
                return MediaTypes.DVD;
            if (Directory.Exists(Path.Combine(path, "BDMV")))
                return MediaTypes.BlueRay;
            if (Directory.Exists(Path.Combine(path, "HVDVD_TS")))
                return MediaTypes.HDDVD;
            return MediaTypes.Unknown;
        }
    }
}
