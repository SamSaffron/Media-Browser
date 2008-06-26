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

    
    public class Config : ModelItem
    {

        /* All app settings go here, they must all have defaults or they will not work properly */
        /* They must be fields and must start with a capitol letter, and should have a default setting */
        /* The comment will be inlined in the config file to help the user */
        
        [Comment(
@"Enable transcode 360 support on extenders"
            )]
        public bool EnableTranscode360 = true;
        [Comment(
@"A lower case comma delimited list of types the extender supports natively. Example: .dvr-ms,.wmv")
         ]
        public string ExtenderNativeTypes = ".dvr-ms,.wmv";

        [Comment(
@" TransparentBackground [Default Value - False]
    True: Enables transparent background. 
    False: Use default Video Browser background."
            )]
        public bool TransparentBackground = false;

        [Comment(
@"Example. If set to true the following will be treated as a movie and an automatic playlist will be created
Indiana Jones / Disc 1 / a.avi 
Indiana Jones / Disc 2 / b.avi")] 
        public bool EnableNestedMovieFolders = true;

        [Comment(
@"Example. If set to true the following will be treated as a movie and an automatic playlist will be created
Indiana Jones / a.avi 
Indiana Jones / b.avi (This only works for 2 videos (no more))
**Setting this to false will override EnableNestedMovieFolders if that is enabled.**"
            )]
        public bool EnableMoviePlaylists = true;

        [Comment(
@"The starting folder for video browser. By default its set to MyVideos. 
Can be set to a folder for example c:\ or a virtual folder for example c:\folder.vf"
            )]
        public string InitialFolder = "MyVideos";

        [Comment(
@"Flag for auto-updates.  True will auto-update, false will not."
            )]
        public bool EnableUpdates = true;

        [Comment(
@"Flag for beta updates.  True will prompt you to update to beta versions."
            )]
        public bool EnableBetas = false;

        [Comment(
@"Set the location of the Daemon Tools binary.."
    )]
        public string DaemonToolsLocation = "C:\\Program Files\\DAEMON Tools Lite\\daemon.exe";

        [Comment(
@"The drive letter of the Daemon Tools virtual drive."
    )]
        public string DaemonToolsDrive = "E";
        
        /* End of app specific settings*/


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

        private Config ()
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
                       BindingFlags.Public | BindingFlags.Instance ))
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
                    value = v.ToString();
                }

                var settingsNode = GetSettingsNode(dom);

                XmlNode node = settingsNode.SelectSingleNode(field.Name);

                if (node == null)
                {
                    settingsNode.AppendChild(dom.CreateComment(GetComment(field)));
                    node = dom.CreateNode(XmlNodeType.Element,field.Name, null);
                    settingsNode.AppendChild(node);
                }
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
