using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.MediaCenter.UI;
using System.IO;
using System.Collections;

namespace MediaBrowser.Library
{
    public class DisplayPreferences : ModelItem
    {
        readonly Choice viewType = new Choice();
        readonly BooleanChoice  showLabels;
        readonly BooleanChoice verticalScroll;
        readonly Choice sortOrders = new Choice();
        readonly Choice indexBy = new Choice();
        readonly BooleanChoice useBanner;
        readonly BooleanChoice useCoverflow;
        SizeRef thumbConstraint = new SizeRef(Config.Instance.DefaultPosterSize);

        public DisplayPreferences(UniqueName ownerName)
        {
            this.OwnerName = ownerName;
            
            ArrayList list = new ArrayList();
            foreach (ViewTypes v in Enum.GetValues(typeof(ViewTypes)))
                list.Add(ViewTypeNames.GetName(v));
            viewType.Options = list;
            
            this.viewType.Chosen = ViewTypeNames.GetName(Config.Instance.DefaultViewType);

            showLabels = new BooleanChoice();
            showLabels.Value = Config.Instance.DefaultShowLabels;

            verticalScroll = new BooleanChoice();
            verticalScroll.Value = Config.Instance.DefaultVerticalScroll;

            useBanner = new BooleanChoice();
            useBanner.Value = false;

            useCoverflow = new BooleanChoice();
            useCoverflow.Value = false;

            ArrayList al = new ArrayList();
            foreach (SortOrder v in Enum.GetValues(typeof(SortOrder)))
                al.Add(SortOrderNames.GetName(v));
            sortOrders.Options = al;
            
            al = new ArrayList();
            foreach (IndexType v in Enum.GetValues(typeof(IndexType)))
                al.Add(IndexTypeNames.GetName(v));
            indexBy.Options = al;
            
            sortOrders.ChosenChanged += new EventHandler(sortOrders_ChosenChanged);
            indexBy.ChosenChanged += new EventHandler(indexBy_ChosenChanged);
            
            viewType.ChosenChanged += new EventHandler(viewType_ChosenChanged);
            showLabels.ChosenChanged += new EventHandler(showLabels_ChosenChanged);
            verticalScroll.ChosenChanged += new EventHandler(verticalScroll_ChosenChanged);
            useBanner.ChosenChanged += new EventHandler(useBanner_ChosenChanged);
            useCoverflow.ChosenChanged += new EventHandler(useCoverflow_ChosenChanged);
            thumbConstraint.PropertyChanged += new PropertyChangedEventHandler(thumbConstraint_PropertyChanged);
        }

        void useCoverflow_ChosenChanged(object sender, EventArgs e)
        {
            Save();
        }

        
        void useBanner_ChosenChanged(object sender, EventArgs e)
        {
            Save();
        }
        
        void indexBy_ChosenChanged(object sender, EventArgs e)
        {
            FirePropertyChanged("IndexBy");
            Save();
        }
        
        void thumbConstraint_PropertyChanged(IPropertyObject sender, string property)
        {
            Save();
        }

        void showLabels_ChosenChanged(object sender, EventArgs e)
        {
            Save();
        }

        void verticalScroll_ChosenChanged(object sender, EventArgs e)
        {
            Save();
        }

        void viewType_ChosenChanged(object sender, EventArgs e)
        {
            switch (ViewTypeNames.GetEnum((string)this.viewType.Chosen))
            {
                case ViewTypes.ThumbStrip:
                    if (this.VerticalScroll.Value)
                        this.VerticalScroll.Chosen = false;
                    if (this.ThumbConstraint.Value.Height > 220)
                        this.ThumbConstraint.Value = new Size(220, 220);
                    break;
                case ViewTypes.CoverFlow:
                    this.ThumbConstraint.Value = new Size(340, 340);
                    break;
            }
            FirePropertyChanged("ViewTypeString");
            Save();
        }
               
        
        void sortOrders_ChosenChanged(object sender, EventArgs e)
        {
            FirePropertyChanged("SortOrder");
            Save();
        }
        

        private bool saveEnabled = true;
        public UniqueName OwnerName { get; set; }
        private const byte Version = 2;
        public void WriteToStream(BinaryWriter bw)
        {
            bw.Write(Version);
            bw.SafeWriteString(ViewTypeNames.GetEnum((string)this.viewType.Chosen).ToString());
            bw.Write(this.showLabels.Value);
            bw.Write(this.verticalScroll.Value);
            bw.SafeWriteString((string)this.SortOrder.ToString());
            bw.SafeWriteString((string)this.IndexBy.ToString());
            bw.Write(this.useBanner.Value);
            bw.Write(this.thumbConstraint.Value.Width);
            bw.Write(this.thumbConstraint.Value.Height);
            bw.Write(this.useCoverflow.Value);
        }

        public static DisplayPreferences ReadFromStream(UniqueName ownerName, BinaryReader br)
        {
            DisplayPreferences dp = new DisplayPreferences(ownerName);
            dp.saveEnabled = false;
            byte version = br.ReadByte();
            try
            {
                dp.viewType.Chosen = ViewTypeNames.GetName((ViewTypes)Enum.Parse(typeof(ViewTypes), br.SafeReadString()));
            }
            catch
            {
                dp.viewType.Chosen = ViewTypeNames.GetName(ViewTypes.Poster);
            }
            dp.showLabels.Value = br.ReadBoolean();
            dp.verticalScroll.Value = br.ReadBoolean();
            try
            {
                dp.SortOrder = (SortOrder)Enum.Parse(typeof(SortOrder), br.SafeReadString());
            }
            catch { }
            dp.IndexBy = (IndexType)Enum.Parse(typeof(IndexType), br.SafeReadString());
            if (!Config.Instance.RememberIndexing)
                dp.IndexBy = IndexType.None;
            dp.useBanner.Value = br.ReadBoolean();
            dp.thumbConstraint.Value = new Size(br.ReadInt32(), br.ReadInt32());
            if (version >= 2)
                dp.useCoverflow.Value = br.ReadBoolean();
            dp.saveEnabled = true;
            return dp;
        }
        
        public Choice SortOrders
        {
            get { return this.sortOrders; }
        }

        public SortOrder SortOrder
        {
            get { return SortOrderNames.GetEnum(sortOrders.Chosen.ToString()); }
            set 
            { 
                this.SortOrders.Chosen = SortOrderNames.GetName(value);
                this.SortOrders.Default = this.SortOrders.Chosen;
            }
        }

        public IndexType IndexBy
        {
            get { return IndexTypeNames.GetEnum(indexBy.Chosen.ToString()); }
            set 
            { 
                this.IndexByChoice.Chosen = IndexTypeNames.GetName(value);
                this.IndexByChoice.Default = this.IndexByChoice.Chosen;
            }
        }

        
        public Choice IndexByChoice
        {
            get { return this.indexBy; }
        }
         
        public Choice ViewType
        {
            get { return this.viewType; }
        }

        public string ViewTypeString
        {
            get { return ViewTypeNames.GetEnum((string)this.viewType.Chosen).ToString(); }
        }

        public BooleanChoice ShowLabels
        {
            get { return this.showLabels; }
        }

        public BooleanChoice VerticalScroll
        {
            get { return this.verticalScroll; }
        }

        public BooleanChoice UseBanner
        {
            get { return this.useBanner; }
        }

        public BooleanChoice UseCoverflow
        {
            get { return this.useCoverflow; }
        }

        
        public SizeRef ThumbConstraint
        {
            get
            {
                return this.thumbConstraint;
            }
        }

        public void IncreaseThumbSize()
        {
            Size s = this.ThumbConstraint.Value;
            s.Height += 20;
            s.Width += 20;
            this.ThumbConstraint.Value = s;
        }

        public void DecreaseThumbSize()
        {
            Size s = this.ThumbConstraint.Value;
            s.Height -= 20;
            s.Width -= 20;
            if (s.Height < 60)
                s.Height = 60;
            if (s.Width < 60)
                s.Width = 60;
            this.ThumbConstraint.Value = s;
        }

        
        
        internal void LoadDefaults()
        {
            
        }

        private void Save()
        {
            if ((!saveEnabled) || (this.OwnerName == null))
                return;
            ItemCache.Instance.SaveDisplayPreferences(this);
        }
        public void ToggleViewTypes()
        {
            this.ViewType.NextValue(true);
            Save();
            FirePropertyChanged("DisplayPrefs");
        }
    }

    public enum SortOrder
    {
        Name,
        Date,
        Rating,
        Runtime,
        Unwatched,
        Year
    }

    public enum IndexType
    {
        None,
        Actor,
        Genre,
        Director,
        Year
    }

    public enum ViewTypes
    {
        CoverFlow,
        Detail,
        Poster,
        Thumb,
        ThumbStrip
    }

    public class ViewTypeNames
    {
        private static readonly string[] Names = { "Cover Flow","Detail", "Poster", "Thumb", "Thumb Strip"};

        public static string GetName(ViewTypes type)
        {
            return Names[(int)type];
        }

        public static ViewTypes GetEnum(string name)
        {
            return (ViewTypes)Array.IndexOf<string>(Names, name);
        }
    }

    public class SortOrderNames
    {
        private static readonly string[] Names = { "name", "date", "rating", "runtime", "unwatched", "year"};

        public static string GetName(SortOrder order)
        {
            return Names[(int)order];
        }

        public static SortOrder GetEnum(string name)
        {
            return (SortOrder)Array.IndexOf<string>(Names, name);
        }
    }

    public class IndexTypeNames
    {
        private static readonly string[] Names = { "none", "actor", "genre", "director","year" };

        public static string GetName(IndexType order)
        {
            return Names[(int)order];
        }

        public static IndexType GetEnum(string name)
        {
            return (IndexType)Array.IndexOf<string>(Names, name);
        }
    }
}
