using System;
using System.Collections.Generic;
using System.Text;
using SamSoft.VideoBrowser.Util;
using System.IO;
using System.Xml.Serialization;
using System.Diagnostics;

namespace SamSoft.VideoBrowser.LibraryManagement
{

    /// <summary>
    /// This class tracks the position, play count and last play date for each video.
    /// </summary>

    public class PlayState
    {
        private static Dictionary<string, PlayState> playStateCache = new Dictionary<string, PlayState>();
       
        public static PlayState Get(IFolderItem folderItem)
        {
            if (folderItem.Key == null)
                return null;
            lock (playStateCache)
                if (playStateCache.ContainsKey(folderItem.Key))
                    return playStateCache[folderItem.Key];
                else
                {
                    PlayState ps = new PlayState(folderItem);
                    playStateCache[folderItem.Key] = ps;
                    return ps;
                }
        }


        public struct PlayStateData
        {
            [XmlElement]
            public int PlaylistPosition;
            [XmlElement]
            public int PlayCount;
            [XmlIgnore]
            public TimeSpan Position
            {
                get
                {
                    return new TimeSpan(PositionTicks);
                }
                set
                {
                    PositionTicks = value.Ticks;
                }
            }
            [XmlElement]
            public long PositionTicks;
            [XmlElement]
            public DateTime LastPlayed;
        }

        private PlayState(IFolderItem folderItem)
        {
            filename = Path.Combine(Helper.AppPlayStatePath,folderItem.Key + ".xml"); 
            Load(); 
        }

        private void Load()
        {
            // in case we fail out;
            playstate = new PlayStateData();

            try
            {
                if (File.Exists(filename))
                {
                    using (FileStream fs = new FileStream(filename, FileMode.Open))
                    {
                        XmlSerializer xs = GetSerializer();
                        playstate = (PlayStateData)xs.Deserialize(fs);
                    }
                }
            }
            catch (Exception e)
            {
                // never crash cause we can not load the state
                Trace.WriteLine("Failed to load play state." + e.ToString());
            }
        }

        private static XmlSerializer GetSerializer()
        {
            XmlSerializer xs = new XmlSerializer(typeof(PlayStateData));
            return xs;
        }

        private void Save()
        {
            try
            {
                using (FileStream fs = new FileStream(filename, FileMode.Create))
                {
                    XmlSerializer xs = GetSerializer();
                    xs.Serialize(fs, playstate);
                }
            }
            catch(Exception e)
            {
                // never crash cause we can not save the state
                Trace.WriteLine("Failed to save play state." + e.ToString());
            }
        }

        private string filename;
        private PlayStateData playstate; 
        

        /// <summary>
        /// Our position in a play list (first movie, second movie etc..) 
        /// </summary>
        public int PlaylistPosition
        {
            get { return playstate.PlaylistPosition; }
            set { playstate.PlaylistPosition = value; Save(); } 
        }

        /// <summary>
        /// How many times was this movie played?
        /// </summary>
        public int PlayCount
        {
            get { return playstate.PlayCount; }
            set { playstate.PlayCount = value; Save(); } 
        }

        public TimeSpan Position 
        {
            get { return playstate.Position;  }
            set { playstate.Position = value; Save(); } 
        }

        public DateTime LastPlayed
        {
            get { return playstate.LastPlayed; }
            set 
            {
                if (playstate.LastPlayed != value)
                {
                    playstate.LastPlayed = value;
                    Save();
                }
            } 
        }
    }
}
