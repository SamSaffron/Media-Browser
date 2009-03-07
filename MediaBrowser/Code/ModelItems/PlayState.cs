using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.MediaCenter.UI;
using System.IO;

namespace MediaBrowser.Library
{
    public class PlayState : ModelItem
    {
        private static readonly byte Version = 1;
        public PlayState()
        {
        }

        public void Assign(UniqueName ownerName)
        {
            this.OwnerName = ownerName;
        }

        public void WriteToStream(BinaryWriter bw)
        {
            bw.Write(Version);
            bw.Write(this.playCount);
            bw.Write(this.positionTicks);
            bw.Write(this.playlistPosition);
            bw.Write(this.lastPlayed.Ticks);
        }

        public static PlayState ReadFromStream(UniqueName ownerName, BinaryReader br)
        {
            byte version = br.ReadByte();
            PlayState ps = PlayStateFactory.Instance.Create(ownerName);
            ps.playCount = br.ReadInt32();
            ps.positionTicks = br.ReadInt64();
            ps.playlistPosition = br.ReadInt32();
            ps.lastPlayed = new DateTime(br.ReadInt64());
            return ps;
        }

        public bool saveEnabled = true;
        public bool HaveWatched
        {
            get { return (this.playCount > 0); }
        }

        public UniqueName OwnerName { get; set; }
        
        private int playCount = -1;
        public int PlayCount
        {
            get { return this.playCount; }
            set
            {
                if (this.playCount != value) 
                {
                    this.playCount = value; 
                    FirePropertyChanged("PlayCount"); 
                    if (playCount <= 1) 
                        FirePropertyChanged("HaveWatched"); 
                    Save(); 
                }
            }
        }

        private long positionTicks;
        public long PositionTicks
        {
            get { return this.positionTicks; }
            set { if (this.positionTicks != value) { this.positionTicks = value; FirePropertyChanged("PositionTicks"); Save(); } }
        }

        public bool CanResume
        {
            get { return this.PositionTicks > 0; }
        }

        private int playlistPosition;
        public int PlaylistPosition
        {
            get { return this.playlistPosition; }
            set { if (this.playlistPosition != value) { this.playlistPosition = value; FirePropertyChanged("PlaylistPosition"); Save(); } }
        }

        private DateTime lastPlayed;
        public DateTime LastPlayed
        {
            get { return this.lastPlayed; }
            set { if (this.lastPlayed != value) { this.lastPlayed = value; FirePropertyChanged("LastPlayed");FirePropertyChanged("LastPlayedString"); Save(); } }
        }

        public string LastPlayedString
        {
            get
            {
                return this.lastPlayed == DateTime.MinValue ? "" : this.LastPlayed.ToShortDateString(); //"Last Watched: " + 
            }
        }
    

        internal void AssignFrom(PlayState data)
        {
            saveEnabled = false;
            try
            {
                this.PlayCount = data.PlayCount;
                this.PositionTicks = data.PositionTicks;
                this.PlaylistPosition = data.PlaylistPosition;
                this.LastPlayed = data.LastPlayed;
            }
            finally
            {
                saveEnabled = true;
            }
        }

        private void Save()
        {
            // todo defer the save for a few seconds to make sure we're not doing it too often
            // but do it on dispose
            if ((!saveEnabled) || (this.OwnerName == null))
                return;
            ItemCache.Instance.SavePlayState( this);
        }

    }
}
