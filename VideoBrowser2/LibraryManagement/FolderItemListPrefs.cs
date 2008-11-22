using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;
using System.IO;
using System.Diagnostics;
using Microsoft.MediaCenter.UI;
using System.Xml.Serialization;
using System.Collections;

namespace SamSoft.VideoBrowser.LibraryManagement
{
    // prefs for the folder item list 
    // sort order and view 
    [Serializable]
    public class FolderItemListPrefs : ModelItem
    {
        string filename;
        Choice viewType;
        BooleanChoice showLabels;
        BooleanChoice verticalScroll;
        bool banners;

        public FolderItemListPrefs(string key)
        {
            viewType = new Choice();
            ArrayList list = new ArrayList();
            foreach (ViewType v in Enum.GetValues(typeof(ViewType)))
                list.Add(ViewTypeNames.GetName(v));
            viewType.Options = list;
            this.ViewType = Config.Instance.DefaultViewType;
            
            showLabels = new BooleanChoice();
            showLabels.Value = Config.Instance.DefaultShowLabels;
            
            verticalScroll = new BooleanChoice();
            verticalScroll.Value = Config.Instance.DefaultVerticalScroll;

            if (this.ViewType == ViewType.Detail)
                banners = Config.Instance.DefaultVerticalScroll;
            else
                banners = false;

            try
            {
                filename = System.IO.Path.Combine(Helper.AppPrefsPath, key + ".prefs.xml");
                if (System.IO.File.Exists(filename))
                {
                    XmlDocument doc = new XmlDocument();
                    doc.Load(filename);
                    // support migration from the old format view files
                    XmlNode node = doc.SelectSingleNode("Prefs/ViewIndex");
                    try
                    {
                        if (node != null)
                        {
                            int i = Int32.Parse(node.InnerText);
                            switch (i)
                            {
                                case 0:
                                    this.ViewType = ViewType.Detail;
                                    break;
                                case 1:
                                    this.ViewType = ViewType.Poster;
                                    this.ShowLabels = false;
                                    break;
                                case 2:
                                    this.ViewType = ViewType.Poster;
                                    this.ShowLabels = true;
                                    break;
                                case 3:
                                    this.ViewType = ViewType.Thumb;
                                    this.ShowLabels = false;
                                    break;
                                case 4:
                                    this.ViewType = ViewType.Thumb;
                                    this.ShowLabels = true;
                                    break;
                            }
                        }
                    }
                    catch { }
                    node = doc.SelectSingleNode("Prefs/SortOrder");
                    try
                    {
                        if (node != null)
                            sortOrder = (SortOrderEnum)(Int32.Parse(node.InnerText));
                    }
                    catch { }
                    node = doc.SelectSingleNode("Prefs/ViewType");
                    try
                    {
                        if (node!=null)
                            ViewType = (ViewType)(Enum.Parse(typeof(ViewType), node.InnerText));
                        if (viewType.Chosen == "Banner")
                            banners = true;
                        else if (viewType.Chosen == "Detail")
                            banners = Config.Instance.DefaultBanners;
                        else
                            banners = false;
                    }
                    catch { }
                    node = doc.SelectSingleNode("Prefs/ShowLabels");
                    try
                    {
                        if (node != null)
                            ShowLabels = (bool.Parse(node.InnerText));
                    }
                    catch { }
                    node = doc.SelectSingleNode("Prefs/VerticalScroll");
                    try
                    {
                        if (node != null)
                            VerticalScroll = (bool.Parse(node.InnerText));
                    }
                    catch { }
                    node = doc.SelectSingleNode("Prefs/ThumbConstraint");
                    try
                    {
                        if (node != null)
                        {
                            string[] sz = node.InnerText.Split(',');
                            Size s = new Size(Int32.Parse(sz[0]), Int32.Parse(sz[1]));
                            this.ThumbConstraint.Value = s;
                        }
                    }
                    catch { }
                }
            }
            catch(Exception ex)
            {
                // corrupt pref file, not a big deal
                Trace.WriteLine("Error reading pref file.\n" + ex.ToString());
                Save();
            }
            viewType.ChosenChanged += new EventHandler(viewType_ChosenChanged);
            showLabels.ChosenChanged += new EventHandler(showLabels_ChosenChanged);
            verticalScroll.ChosenChanged += new EventHandler(verticalScroll_ChosenChanged);
            thumbConstraint.PropertyChanged += new PropertyChangedEventHandler(thumbConstraint_PropertyChanged);
        }

        void thumbConstraint_PropertyChanged(IPropertyObject sender, string property)
        {
            Save();
            FirePropertyChanged("ThumbConstraint");
        }

        void showLabels_ChosenChanged(object sender, EventArgs e)
        {
            Save();
            FirePropertyChanged("ShowLabels");
        }

        void verticalScroll_ChosenChanged(object sender, EventArgs e)
        {
            Save();
            FirePropertyChanged("VerticalScroll");
        }

        void viewType_ChosenChanged(object sender, EventArgs e)
        {
            if (viewType.Chosen == "Banner")
                banners = true;
            else if (viewType.Chosen == "Detail")
                banners = Config.Instance.DefaultBanners;
            else
                banners = false;
            Save();
            FirePropertyChanged("ViewType");
            FirePropertyChanged("ViewTypeString");
            FirePropertyChanged("Banners");
        }

        
        [XmlIgnore]
        public Choice ViewTypeChoice
        {
            get { return this.viewType; }
        }

        [XmlElement]
        public ViewType ViewType 
        {
            get
            {
                return ViewTypeNames.GetEnum((string)this.viewType.Chosen);
            }
            set
            {
                string name = ViewTypeNames.GetName(value);
                if (this.viewType.Chosen != name)
                    this.viewType.Chosen = name;
            }
        }

        [XmlIgnore]
        public string ViewTypeString
        {
            get { return this.ViewType.ToString(); }
        }

        [XmlIgnore]
        public BooleanChoice ShowLabelsChoice
        {
            get { return this.showLabels; }
        }

        [XmlElement]
        public bool ShowLabels 
        { 
            get
            {
                return this.showLabels.Value;
            }
            set
            {
                if (this.showLabels.Value != value)
                    this.showLabels.Value = value;
                
            }
        }

        [XmlIgnore]
        public BooleanChoice VerticalScrollChoice
        {
            get { return this.verticalScroll; }
        }

        [XmlElement]
        public bool VerticalScroll
        {
            get
            {
                return this.verticalScroll.Value;
            }
            set
            {
                if (this.verticalScroll.Value != value)
                    this.verticalScroll.Value = value;
            }
        }

        [XmlElement]
        public bool Banners
        {
            get
            {
                return this.banners;
            }
            set
            {
                if (this.banners != value)
                {
                    this.banners = value;
                    Save();
                    FirePropertyChanged("Banners");
                }
            }
        }


        SortOrderEnum sortOrder = SortOrderEnum.Name; 
        public SortOrderEnum SortOrder
        {
            get
            {
                return sortOrder; 
            }
            set
            {
                if (sortOrder != value)
                {
                    sortOrder = value;
                    Save();
                    FirePropertyChanged("SortOrder");
                }
            }
        }

        SizeRef thumbConstraint = new SizeRef(Config.Instance.DefaultPosterSize);
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
         
        public void Save() 
        {
            try
            {
                try
                {
                    MemoryStream ms = new MemoryStream();
                    XmlWriterSettings settings = new XmlWriterSettings();
                    settings.Encoding = Encoding.UTF8;
                    settings.Indent = true;
                    settings.IndentChars = "\t";
                    XmlWriter writer = XmlWriter.Create(ms, settings);
                    writer.WriteStartDocument();
                    writer.WriteStartElement("Prefs");
                    writer.WriteElementString("SortOrder", ((int)SortOrder).ToString());
                    writer.WriteElementString("ViewType", ViewType.ToString());
                    writer.WriteElementString("ShowLabels", ShowLabels.ToString());
                    writer.WriteElementString("VerticalScroll", VerticalScroll.ToString());
                    Size s = this.ThumbConstraint.Value;
                    writer.WriteElementString("ThumbConstraint", s.Width.ToString() + "," + s.Height.ToString());
                    writer.WriteEndElement();
                    writer.Close();
                    ms.Flush();
                    File.WriteAllBytes(filename, ms.ToArray());
                }
                catch(Exception ex)
                {
                    // not a huge deal, prefs did not save. 
                    Trace.WriteLine("Error saving pref file.\n" + ex.ToString());
                }
            }
            catch (Exception ex)
            {
                // not a huge deal, prefs did not save. 
                Trace.TraceInformation("Error saving pref file.\n" + ex.ToString());
            }
        }
    }

    public enum ViewType
    {
        Detail,
        Poster,
        Thumb,
        Banner
    }

    public class ViewTypeNames
    {
        private static readonly string[] Names = { "Detail", "Poster", "Thumb Strip", "Banner"};

        public static string GetName(ViewType type)
        {
            return Names[(int)type];
        }

        public static ViewType GetEnum(string name)
        {
            return (ViewType)Array.IndexOf<string>(Names, name);
        }
    }
}
