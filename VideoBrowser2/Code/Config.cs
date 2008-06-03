using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Configuration;
using System.Reflection;
using System.Xml;
using SamSoft.VideoBrowser.LibraryManagement;

namespace SamSoft.VideoBrowser
{
    [global::System.AttributeUsage(AttributeTargets.Field, Inherited = false, AllowMultiple = false)]
    sealed class DefaultAttribute : Attribute
    {
        // See the attribute guidelines at 
        //  http://go.microsoft.com/fwlink/?LinkId=85236
        readonly object value;

        public DefaultAttribute(object value)
        {
            this.value = value;
        }

        public string Value { get; private set; }

        // This is a named argument
        public int NamedInt { get; set; }
    }

    public class Config
    {

        /* All app settings go here, they must all have defaults or they will not work properly */
        /* They must be fields and must start with a capitol letter */ 
        [Default(false)]
        public bool EnableTranscode360;


        /* End of app specific settings*/

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

        private Config ()
	    {

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
                File.WriteAllText(filename, "<settings></settings>");
                Write();
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

            MemberInfo[] fields = this.GetType().GetMembers(
                       BindingFlags.Public | BindingFlags.Instance );

            foreach(MemberInfo member in fields)
            {
                FieldInfo field = null;
                
                if (IsSettingField(member))
                {
                    field = (FieldInfo) member;
                }
                else
                {
                    continue; 
                }

                var settingsNode = dom.SelectSingleNode("/settings");
                if (settingsNode == null)
                {
                    settingsNode = dom.CreateNode(XmlNodeType.Element, "settings", null);
                }

                XmlNode node = settingsNode.SelectSingleNode(field.Name);

                if (node == null)
                {
                    node = dom.CreateNode(XmlNodeType.Element, field.Name, null);
                    settingsNode.AppendChild(node);
                    stuff_changed = true;
                    node.InnerText = Default(member).ToString(); 
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
                        field.SetValue(this, Default(member));
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



        /// <summary>
        /// Write current config to file
        /// </summary>
        public void Write()
        {
         
            XmlDocument dom = new XmlDocument();
            dom.Load(filename); 

            foreach (MemberInfo field in this.GetType().GetMembers())
            {
                if (!IsSettingField(field))
                {
                    // don't persist lower fields and properties
                    continue;
                }

                string value = null;
                if (field.MemberType == MemberTypes.Field)
                {
                    object v = ((FieldInfo)field).GetValue(this);
                    if (v == null)
                    {
                        v = Default(field);
                    }
                    else
                    {
                        value = v.ToString(); 
                    }
                }

                else
                {
                    continue; // not a property or field
                }

                var settingsNode = dom.SelectSingleNode("/settings");
                if (settingsNode == null)
                {
                    settingsNode = dom.CreateNode(XmlNodeType.Element, "settings", null);
                }

                XmlNode node = settingsNode.SelectSingleNode(field.Name);

                if (node == null)
                {
                    node = dom.CreateNode(XmlNodeType.Element,field.Name, null);
                    settingsNode.AppendChild(node);
                }
                node.InnerText = value;
            } // for each
            dom.Save(filename);
        }

        private static bool IsSettingField(MemberInfo field)
        {
            return field.Name[0].ToString() != field.Name[0].ToString().ToLower();
        }

        private object Default(MemberInfo field)
        {
            //TODO: some nice error handling
            DefaultAttribute da = (DefaultAttribute)field.GetCustomAttributes(typeof(DefaultAttribute), false)[0];
            return da.Value;
        }
    }
}
