using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;
using System.IO;
using System.Diagnostics;

namespace SamSoft.VideoBrowser.LibraryManagement
{
    // prefs for the folder item list 
    // sort order and view 
    public class FolderItemListPrefs
    {
        string filename; 

        public FolderItemListPrefs(string key)
        {
            filename = System.IO.Path.Combine(Helper.AppDataPath, key + ".prefs.xml");
            try
            {
                if (System.IO.File.Exists(filename))
                {
                    XmlDocument doc = new XmlDocument();
                    doc.Load(filename);
                    _sortOrder = (SortOrderEnum)(Int32.Parse(doc.SelectSingleNode("Prefs/SortOrder").InnerText));
                }
            }
            catch
            {
                // corrupt pref file, not a big deal
                Trace.WriteLine("Error reading pref file");
            }
        }

        SortOrderEnum _sortOrder = SortOrderEnum.Name; 
        public SortOrderEnum SortOrder
        {
            get
            {
                return _sortOrder; 
            }
            set
            {
                _sortOrder = value;
            }
        } 
       
        // TODO : view in here 
 
        public void Save() 
        {
            try
            {
                MemoryStream ms = new MemoryStream();
                XmlWriter writer = new XmlTextWriter(ms, Encoding.UTF8);
                writer.WriteStartDocument();
                writer.WriteStartElement("Prefs");
                writer.WriteElementString("SortOrder", ((int)SortOrder).ToString());
                writer.WriteEndElement();
                writer.Close();
                ms.Flush();
                File.WriteAllBytes(filename, ms.ToArray());
            }
            catch
            {
                // not a huge deal, prefs did not save. 
                Trace.WriteLine("Error saving pref file");
            }
        }
    }
}
