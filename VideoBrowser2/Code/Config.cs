using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Configuration;
using System.Reflection;
using System.Xml;
using SamSoft.VideoBrowser.LibraryManagement;

using Microsoft.MediaCenter.UI;

namespace SamSoft.VideoBrowser
{
    [global::System.AttributeUsage(AttributeTargets.Field, Inherited = false, AllowMultiple = false)]
    sealed class CommentAttribute : Attribute
    {

        // This is a positional argument
        public CommentAttribute(string comment)
        {
            Comment = comment;
        }

        public string Comment { get; private set; }
    }


    public class Config
    {

        /* All app settings go here, they must all have defaults or they will not work properly */
        /* They must be fields and must start with a capital letter, and should have a default setting */
        /* The comment will be inlined in the config file to help the user */

        [Comment(@"Enables you to scan the display to cope with overscan issue, parameter should be of the for x,y,z scaling factors")]
        public Vector3 OverScanScaling = new Vector3(1, 1, 1);
        public Vector3 OverScanScalingMcml
        {
            get { return this.OverScanScaling; }
        }

        [Comment("Defines padding to apply round the edge of the screen to cope with overscan issues")]
        public Inset OverScanPadding = new Inset(0, 0, 0, 0);
        public Inset OverScanPaddingMcml
        {
            get { return this.OverScanPadding; }
        }

        [Comment(@"Enables the writing of trace log files in a production environment to assist with problem solving")]
        public bool EnableTraceLogging = false;

        [Comment(@"The default size of posters before change are made to the view settings")]
        public Size DefaultPosterSize = new Size(220, 220);

        [Comment("Controls the space between items in the poster and thumb strip views")]
        public Size GridSpacing = new Size(0, 0);
        public Size GridSpacingMcml
        {
            get { return this.GridSpacing; }
        }

        [Comment(@"Enable transcode 360 support on extenders")]
        public bool EnableTranscode360 = true;
        [Comment(@"A lower case comma delimited list of types the extender supports natively. Example: .dvr-ms,.wmv")]
        public string ExtenderNativeTypes = ".dvr-ms,.wmv";

        [Comment("TransparentBackground [Default Value - False]\n\tTrue: Enables transparent background.\n\tFalse: Use default Video Browser background.")]
        public bool TransparentBackground = false;

        [Comment("Example. If set to true the following will be treated as a movie and an automatic playlist will be created.\n\tIndiana Jones / Disc 1 / a.avi\n\tIndiana Jones / Disc 2 / b.avi")]
        public bool EnableNestedMovieFolders = true;

        [Comment("Example. If set to true the following will be treated as a movie and an automatic playlist will be created.\n\tIndiana Jones / a.avi\n\tIndiana Jones / b.avi (This only works for 2 videos (no more))\n**Setting this to false will override EnableNestedMovieFolders if that is enabled.**")]
        public bool EnableMoviePlaylists = true;

        [Comment("The starting folder for video browser. By default its set to MyVideos.\nCan be set to a folder for example c:\\ or a virtual folder for example c:\\folder.vf")]
        public string InitialFolder = "MyVideos";

        [Comment(@"Flag for auto-updates.  True will auto-update, false will not.")]
        public bool EnableUpdates = true;

        [Comment(@"Flag for beta updates.  True will prompt you to update to beta versions.")]
        public bool EnableBetas = false;

        [Comment(@"Set the location of the Daemon Tools binary..")]
        public string DaemonToolsLocation = "C:\\Program Files\\DAEMON Tools Lite\\daemon.exe";

        [Comment(@"The drive letter of the Daemon Tools virtual drive.")]
        public string DaemonToolsDrive = "E";

        [Comment("Flag for alphanumeric sorting.  True will use alphanumeric sorting, false will use alphabetic sorting.\nNote that the sorting algorithm is case insensitive.")]
        public bool EnableAlphanumericSorting = true;

        [Comment(@"Enables the showing of tick in the list view for files that have been watched")]
        public bool EnableListViewTicks = false;
        public bool EnableListViewTicksMcml { get { return this.EnableListViewTicks; } }

        [Comment(@"Enables the showing of watched shows in a different color in the list view (Transparent disables it)")]
        public Colors ListViewWatchedColor = Colors.LightSkyBlue;
        public Color ListViewWatchedColorMcml
        {
            get { return new Color(this.ListViewWatchedColor); }
        }

        [Comment("Enables the views to default to the first unwatched item in a folder of movies or tv shows")]
        public bool DefaultToFirstUnwatched = false;

        [Comment(@"Indicates that files with a date stamp before this date should be assumed to have been watched for the purpose of ticking them off.")]
        public DateTime AssumeWatchedBefore = DateTime.MinValue;

        [Comment("Changes the default view index for folders that have not yet been visited.\n\t[Detail|Poster|Thumb]")]
        public ViewType DefaultViewType = ViewType.Detail;

        [Comment("Specifies whether the default Poster and Thumb views show labels")]
        public bool DefaultShowLabels = false;

        [Comment("Specifies is the default for the Poster view is vertical scrolling")]
        public bool DefaultVerticalScroll = false;

        [Comment(@"Limits the number of levels shown by the breadcrumbs.")]
        public int BreadcrumbCountLimit = 2;

        [Comment("List of characters to remove from titles for alphanumeric sorting.  Separate each character with a '|'.\nThis allows titles like '10,000.BC.2008.720p.BluRay.DTS.x264-hV.mkv' to be properly sorted.")]
        public string SortRemoveCharacters = ",|&|-";

        [Comment("List of characters to replace with a ' ' in titles for alphanumeric sorting.  Separate each character with a '|'.\nThis allows titles like 'Iron.Man.REPACK.720p.BluRay.x264-SEPTiC.mkv' to be properly sorted.")]
        public string SortReplaceCharacters = ".|+|%";

        // TODO: Might need REAL "replacers", i.e. replace & with "and" and replace % with "percent"
        //       Could do Roman Numerals here as well

        [Comment(@"List of words to remove from alphanumeric sorting.  Separate each word with a '|'.  Note that the
        algorithm appends a ' ' to the end of each word during the search which means words found at the end
        of each title will not be removed.  This is generally not an issue since most people will only want
        articles removed and articles are rarely found at the end of media titles.  This, combined with SortReplaceCharacters,
        allows titles like 'The.Adventures.Of.Baron.Munchausen.1988.720p.BluRay.x264-SiNNERS.mkv' to be properly sorted.")]
        public string SortReplaceWords = "the|a|an";


        /* End of app specific settings*/

        private string[] _SortRemoveCharactersArray = null;
        public string[] SortRemoveCharactersArray
        {
            get
            {
                _SortRemoveCharactersArray = _SortRemoveCharactersArray ?? SortRemoveCharacters.Split('|');
                return _SortRemoveCharactersArray;
            }
        }

        private string[] _SortReplaceCharactersArray = null;
        public string[] SortReplaceCharactersArray
        {
            get
            {
                _SortReplaceCharactersArray = _SortReplaceCharactersArray ?? SortReplaceCharacters.Split('|');
                return _SortReplaceCharactersArray;
            }
        }

        private string[] _SortReplaceWordsArray = null;
        public string[] SortReplaceWordsArray
        {
            get
            {
                _SortReplaceWordsArray = _SortReplaceWordsArray ?? SortReplaceWords.Split('|');
                return _SortReplaceWordsArray;
            }
        }

        // so we can access settings from mcml 
        [MarkupVisible]
        public bool TransparentBackgroundMCML
        {
            get
            {
                return TransparentBackground;
            }
        }


        private static object _syncobj = new object();
        private static Config _instance = null;
        public static Config Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_syncobj)
                    {
                        if (_instance == null)
                        {
                            _instance = new Config();
                        }
                    }
                }
                return _instance;
            }
        }

        string filename;

        private Dictionary<string, object> defaults = new Dictionary<string, object>();

        private Config()
        {

            // store the defaults so we can later recover them if needed
            foreach (FieldInfo field in SettingFields)
            {
                defaults[field.Name] = field.GetValue(this);
            }

            var path = Helper.AppConfigPath;
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }

            filename = Path.Combine(path, "VideoBrowser.config");
            try
            {
                Read();
            }
            catch
            {
                File.WriteAllText(filename, "<Settings></Settings>");
                Write();
            }
        }


        private List<FieldInfo> SettingFields
        {
            get
            {
                // todo: cache this, not really important 
                List<FieldInfo> fields = new List<FieldInfo>();
                foreach (MemberInfo mi in this.GetType().GetMembers(
                       BindingFlags.Public | BindingFlags.Instance))
                {
                    if (IsSettingField(mi))
                    {
                        fields.Add((FieldInfo)mi);
                    }
                }
                return fields;
            }
        }

        /// <summary>
        /// Read current config from file
        /// </summary>
        public void Read()
        {
            bool stuff_changed = false;

            XmlDocument dom = new XmlDocument();
            dom.Load(filename);

            foreach (FieldInfo field in SettingFields)
            {

                var settingsNode = GetSettingsNode(dom);

                XmlNode node = settingsNode.SelectSingleNode(field.Name);

                if (node == null)
                {
                    node = dom.CreateNode(XmlNodeType.Element, field.Name, null);
                    settingsNode.AppendChild(node);
                    node.InnerText = Default(field).ToString();
                    stuff_changed = true;
                }

                string value = node.InnerText;

                if (field.FieldType == typeof(string))
                {
                    field.SetValue(this, value);
                }
                else if (field.FieldType == typeof(bool))
                {
                    try
                    {
                        field.SetValue(this, bool.Parse(value));
                    }
                    catch
                    {
                        field.SetValue(this, Default(field));
                        stuff_changed = true;
                    }
                }
                else if (field.FieldType == typeof(DateTime))
                {
                    DateTime dt;
                    if (DateTime.TryParse(value, out dt))
                        field.SetValue(this, dt);
                    else
                        stuff_changed = true;
                }
                else if (field.FieldType == typeof(int))
                {
                    int x;
                    if (Int32.TryParse(value, out x))
                        field.SetValue(this, x);
                    else
                        stuff_changed = true;
                }
                else if (field.FieldType == typeof(Size))
                {
                    try
                    {
                        string[] parts = value.Split(',');
                        Size s = new Size(Int32.Parse(parts[0]), Int32.Parse(parts[1]));
                        field.SetValue(this, s);
                    }
                    catch
                    {
                        stuff_changed = true;
                    }
                }
                else if (field.FieldType == typeof(Inset))
                {
                    try
                    {
                        string[] parts = value.Split(',');
                        Inset s = new Inset(Int32.Parse(parts[0]), Int32.Parse(parts[1]), Int32.Parse(parts[2]), Int32.Parse(parts[3]));
                        field.SetValue(this, s);
                    }
                    catch
                    {
                        stuff_changed = true;
                    }
                }
                else if (field.FieldType == typeof(Colors))
                {
                    try
                    {
                        Colors c = (Colors)Enum.Parse(typeof(Colors), value);
                        field.SetValue(this, c);
                    }
                    catch
                    {

                    }
                }
                else if (field.FieldType == typeof(Vector3))
                {
                    try
                    {
                        string[] parts = value.Split(',');
                        Vector3 s = new Vector3(float.Parse(parts[0]), float.Parse(parts[1]), float.Parse(parts[2]));
                        field.SetValue(this, s);
                    }
                    catch { }
                }
                else if (field.FieldType == typeof(ViewType))
                {
                    try
                    {
                        ViewType c = (ViewType)Enum.Parse(typeof(ViewType), value);
                        field.SetValue(this, c);
                    }
                    catch
                    {

                    }
                }
                else
                {
                    // only supporting above types for now
                    return;
                }
            }

            if (stuff_changed)
            {
                Write();
            }
        }


        private static XmlNode GetSettingsNode(XmlDocument dom)
        {
            return dom.SelectSingleNode("/Settings");
        }

        /// <summary>
        /// Write current config to file
        /// </summary>
        public void Write()
        {
            XmlDocument dom = new XmlDocument();
            dom.Load(filename);
            var settingsNode = GetSettingsNode(dom);
            settingsNode.RemoveAll();

            foreach (FieldInfo field in SettingFields)
            {

                string value = "";
                object v = field.GetValue(this);
                if (v == null)
                {
                    v = Default(field);
                }
                if (v != null)
                {
                    if (field.FieldType == typeof(Size))
                    {
                        Size s = (Size)v;
                        value = s.Width + "," + s.Height;
                    }
                    else if (field.FieldType == typeof(Vector3))
                    {
                        Vector3 s = (Vector3)v;
                        value = s.X + "," + s.Y + "," + s.Z;
                    }
                    else if (field.FieldType == typeof(Inset))
                    {
                        Inset s = (Inset)v;
                        value = s.Left + "," + s.Top + "," + s.Right + "," + s.Bottom;
                    }
                    else
                        value = v.ToString();
                }

                settingsNode.AppendChild(dom.CreateComment(GetComment(field)));
                XmlNode node = dom.CreateNode(XmlNodeType.Element, field.Name, null);
                settingsNode.AppendChild(node);
                node.InnerText = value;
            } // for each
            dom.Save(filename);
        }

        private string GetComment(MemberInfo field)
        {
            string comment = "";
            var attribs = field.GetCustomAttributes(typeof(CommentAttribute), false);
            if (attribs != null && attribs.Length > 0)
            {
                comment = ((CommentAttribute)attribs[0]).Comment;
            }
            return comment;
        }

        private static bool IsSettingField(MemberInfo mi)
        {
            return mi.Name[0].ToString() != mi.Name[0].ToString().ToLower() && mi.MemberType == MemberTypes.Field;
        }

        private object Default(MemberInfo field)
        {
            //TODO: some nice error handling
            // DefaultAttribute da = (DefaultAttribute)field.GetCustomAttributes(typeof(DefaultAttribute), false)[0];
            return defaults[field.Name];
        }
    }
}
