using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.MediaCenter.UI;
using System.IO;

namespace MediaBrowser.Library
{
    public class Actor
    {
        public string Name { get; set; }
        public string Role { get; set; }

        public string DisplayName
        {
            get
            {
                if (string.IsNullOrEmpty(this.Role))
                    return Name;
                else
                    return Name ;
            }
        }

        public Boolean HasRole
        {
            get
            {
                return (Role != null);
            }
        }

        public String DisplayRole
        {
            get
            {
                if (Role != null)
                {
                    return Role;
                }
                else
                    return "";
            }
        }

        public string DisplayString
        {
            get
            {
                if (string.IsNullOrEmpty(this.Role))
                    return Name;
                else
                    return Name + "..." + Role;
            }
        }
    }

    public class MediaMetadataStore
    {


        private static readonly byte Version = 4;
        public MediaMetadataStore(UniqueName ownerName)
        {
            this.OwnerName = ownerName;
            this.ProviderData = new Dictionary<string, string>();
        }

        public void WriteToStream(BinaryWriter bw)
        {
            bw.Write(Version);
            bw.SafeWriteString(this.Name);
            bw.SafeWriteString(this.SubName);
            bw.SafeWriteString(this.Overview);
            bw.SafeWriteString(this.SeasonNumber);
            bw.SafeWriteString(this.EpisodeNumber);
            if (this.ImdbRating != null)
            {
                bw.Write(true);
                bw.Write(this.ImdbRating.Value);
            }
            else
                bw.Write(false);
            if (this.ProductionYear != null)
            {
                bw.Write(true);
                bw.Write(this.ProductionYear.Value);
            }
            else
                bw.Write(false);
            if (this.RunningTime != null)
            {
                bw.Write(true);
                bw.Write(this.RunningTime.Value);
            }
            else
                bw.Write(false);
            if (this.UtcDataTimestamp != null)
            {
                bw.Write(true);
                bw.Write(this.UtcDataTimestamp.Value.Ticks);
            }
            else
                bw.Write(false);
            bw.SafeWriteString(this.DataSource);
            lock (this.ProviderData)
            {
                bw.Write(this.ProviderData.Count);
                foreach (KeyValuePair<string, string> kv in this.ProviderData)
                {
                    bw.Write(kv.Key);
                    bw.SafeWriteString(kv.Value);
                }
            }
            WriteList(bw, this.Directors);
            WriteList(bw, this.Actors);
            WriteList(bw, this.Genres);
            WriteImageSource(bw, this.PrimaryImage);
            WriteImageSource(bw, this.SecondaryImage);
            WriteImageSource(bw, this.BannerImage);
            WriteImageSource(bw, this.BackdropImage);
            bw.SafeWriteString(this.MpaaRating);
            WriteList(bw, this.Writers);
            bw.Write(this.MediaInfo != null);
            if (this.MediaInfo != null)
                this.MediaInfo.Write(bw);
        }

        private static void WriteImageSource(BinaryWriter bw, ImageSource imageSource)
        {
            bw.Write((imageSource != null));
            if (imageSource != null)
                imageSource.WriteToStream(bw);
        }

        private static void WriteList(BinaryWriter bw, List<string> data)
        {
            if (data != null)
            {
                bw.Write(data.Count);
                foreach (string s in data)
                    bw.SafeWriteString(s);
            }
            else
                bw.Write((int)0);
        }

        private static void WriteList(BinaryWriter bw, List<Actor> data)
        {
            if (data != null)
            {
                bw.Write(data.Count);
                foreach (Actor s in data)
                {
                    bw.SafeWriteString(s.Name);
                    bw.SafeWriteString(s.Role);
                }

            }
            else
                bw.Write((int)0);
        }

        public static MediaMetadataStore ReadFromStream(UniqueName owner, BinaryReader br)
        {
            MediaMetadataStore store = new MediaMetadataStore(owner);
            byte v = br.ReadByte();
            store.Name = br.SafeReadString();
            store.SubName = br.SafeReadString();
            store.Overview = br.SafeReadString();
            store.SeasonNumber = br.SafeReadString();
            store.EpisodeNumber = br.SafeReadString();
            if (br.ReadBoolean())
                store.ImdbRating = br.ReadSingle();
            if (br.ReadBoolean())
                store.ProductionYear = br.ReadInt32();
            if (br.ReadBoolean())
                store.RunningTime = br.ReadInt32();
            if (br.ReadBoolean())
                store.UtcDataTimestamp = new DateTime(br.ReadInt64());
            store.DataSource = br.SafeReadString();
            int count = br.ReadInt32();
            for (int i = 0; i < count; ++i)
                store.ProviderData[br.ReadString()] = br.SafeReadString();
            store.Directors = ReadList(br);
            store.Actors = ReadActorList(br);
            store.Genres = ReadList(br);
            store.PrimaryImage = ReadImageSource(br);
            store.SecondaryImage = ReadImageSource(br);
            store.BannerImage = ReadImageSource(br);
            store.BackdropImage = ReadImageSource(br);
            store.MpaaRating = br.SafeReadString();
            store.Writers = ReadList(br);
            if (br.ReadBoolean())
                store.MediaInfo = MediaInfoData.FromStream(br);
            return store;
        }

        private static ImageSource ReadImageSource(BinaryReader br)
        {
            bool present = br.ReadBoolean();
            if (present)
                return ImageSource.ReadFromStream(br);
            else
                return null;
        }

        private static List<string> ReadList(BinaryReader br)
        {
            int len = br.ReadInt32();
            if (len > 0)
            {
                List<string> ret = new List<string>();
                for (int i = 0; i < len; ++i)
                    ret.Add(br.SafeReadString());
                return ret;
            }
            return null;
        }

        private static List<Actor> ReadActorList(BinaryReader br)
        {
            int len = br.ReadInt32();
            if (len > 0)
            {
                List<Actor> ret = new List<Actor>();
                for (int i = 0; i < len; ++i)
                    ret.Add(new Actor { Name = br.SafeReadString(), Role = br.SafeReadString() });
                return ret;
            }
            return null;
        }

        /// <summary>
        /// Merges the data from the provided object onto this one replacing entries that are currently null
        /// It will not overwrite non-null entries
        /// </summary>
        /// <param name="data"></param>
        internal void Merge(MediaMetadataStore data)
        {
            if (this.Name == null)
                this.Name = data.Name;
            if (this.SubName == null)
                this.SubName = data.SubName;
            if (this.Overview == null)
                this.Overview = data.Overview;
            if (this.PrimaryImage == null)
                this.PrimaryImage = data.PrimaryImage;
            if (this.SecondaryImage == null)
                this.SecondaryImage = data.SecondaryImage;
            if (this.BannerImage == null)
                this.BannerImage = data.BannerImage;
            if (this.BackdropImage == null)
                this.BackdropImage = data.BackdropImage;
            if (this.SeasonNumber == null)
                this.SeasonNumber = data.SeasonNumber;
            if (this.EpisodeNumber == null)
                this.EpisodeNumber = data.EpisodeNumber;
            if (this.ImdbRating == null)
                this.ImdbRating = data.ImdbRating;
            if (this.ProductionYear == null)
                this.ProductionYear = data.ProductionYear;
            if (this.RunningTime == null)
                this.RunningTime = data.RunningTime;
            if (this.Directors == null)
                this.Directors = data.Directors;
            if (this.Writers == null)
                this.Writers = data.Writers;
            if (this.Actors == null)
                this.Actors = data.Actors;
            if (this.Genres == null)
                this.Genres = data.Genres;
            //if (this.UtcDataTimestamp == null)
                //this.UtcDataTimestamp = data.UtcDataTimestamp;
            if (this.DataSource == null)
                this.DataSource = data.DataSource;
            if (this.MpaaRating == null)
                this.MpaaRating = data.MpaaRating;
            if (this.MediaInfo == null)
                this.MediaInfo = data.MediaInfo;

            foreach (KeyValuePair<string, string> kv in data.ProviderData)
                lock (this.ProviderData)
                    this.ProviderData[kv.Key] = kv.Value;

        }

        public UniqueName OwnerName { get; set; }
        public string Name { get; set; }
        public string SubName { get; set; }
        public string Overview { get; set; }
        public string SeasonNumber { get; set; }
        public string EpisodeNumber { get; set; }
        public float? ImdbRating { get; set; }
        public int? ProductionYear { get; set; }
        public int? RunningTime { get; set; }
        public DateTime? UtcDataTimestamp { get; set; }
        public string DataSource { get; set; }
        public Dictionary<string, string> ProviderData { get; set; }

        public List<string> Directors { get; set; }
        public List<string> Writers { get; set; }
        public List<Actor> Actors { get; set; }
        public List<string> Genres { get; set; }

        public ImageSource PrimaryImage { get; set; }
        public ImageSource SecondaryImage { get; set; }
        public ImageSource BannerImage { get; set; }
        public ImageSource BackdropImage { get; set; }
        public string MpaaRating { get; set; }
        public MediaInfoData MediaInfo { get; set; }
        /// <summary>
        /// data that can be stored by the provider of the data to assist it with refreshing / updating
        /// </summary>

    }

    public class MediaInfoData
    {
        public readonly static MediaInfoData Empty = new MediaInfoData { AudioFormat = "", VideoCodec = "" };

        private static byte Version = 1;
        public int Height;
        public int Width;
        public string VideoCodec;
        public string AudioFormat;
        public int VideoBitRate;
        public int AudioBitRate;

        public string CombinedInfo
        {
            get 
            { 
                if (this!=Empty)
                    return string.Format("{0}x{1}, {2} {3}kbps, {4} {5}kbps", this.Width, this.Height, this.VideoCodec, this.VideoBitRate/1000, this.AudioFormat, this.AudioBitRate/1000); 
                else 
                    return "";
            }
        }

        public void Write(BinaryWriter bw)
        {
            bw.Write(Version);
            bw.Write(Height);
            bw.Write(Width);
            bw.SafeWriteString(VideoCodec);
            bw.SafeWriteString(AudioFormat);
            bw.Write(VideoBitRate);
            bw.Write(AudioBitRate);
            
        }

        public static MediaInfoData FromStream(BinaryReader br)
        {
            byte v = br.ReadByte();
            return new MediaInfoData
            {
                Height = br.ReadInt32(),
                Width = br.ReadInt32(),
                VideoCodec = br.SafeReadString(),
                AudioFormat = br.SafeReadString(),
                VideoBitRate = br.ReadInt32(),
                AudioBitRate = br.ReadInt32()
            };
        }
    }


}
