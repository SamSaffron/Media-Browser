using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;
using System.Diagnostics;

namespace System.Runtime.CompilerServices
{
    public class ExtensionAttribute : Attribute
    { }
}

namespace System.Xml
{
    public static class MyExtension
    {

        public static int SafeGetInt(this XmlDocument doc, string path)
        {
            return SafeGetInt(doc, path, 0);
        }

        public static int SafeGetInt(this XmlDocument doc, string path, int defaultInt)
        {
            XmlNode rvalNode = doc.SelectSingleNode(path);
            if (rvalNode != null && rvalNode.InnerText.Length > 0)
            {
                int rval;
                if (Int32.TryParse(rvalNode.InnerText, out rval))
                {
                    return rval; 
                }
                 
            }
            return defaultInt;
        }

        public static float SafeGetFloat(this XmlDocument doc, string path, float minValue, float maxValue)
        {
            XmlNode rvalNode = doc.SelectSingleNode(path);
            if (rvalNode != null && rvalNode.InnerText.Length > 0)
            {
                float rval;
                if (float.TryParse(rvalNode.InnerText, out rval))
                {
                    if (rval >= minValue && rval <= maxValue)
                    {
                        return rval;
                    }
                }

            }
            return minValue;
        }


        public static string SafeGetString(this XmlDocument doc, string path)
        {
            return SafeGetString(doc, path, ""); 
        }

        public static string SafeGetString(this XmlDocument doc, string path, string defaultString)
        {
            XmlNode rvalNode = doc.SelectSingleNode(path);
            if (rvalNode != null && rvalNode.InnerText.Length > 0)
            {
                return rvalNode.InnerText;
            }
            return defaultString;
        }
    }
    
}

namespace SamSoft.VideoBrowser.LibraryManagement
{
    public class Actor
    {
        public Actor(string name, string role)
        {
            Name = name;
            Role = role;
        }

        public string Name { get; set; }
        public string Role { get; set; }

        public override string ToString()
        {
            return Name + " ... " + Role;
        }
    }

    public class Movie
    {
        public string Description { get; private set; }
        public int RunningTime { get; private set; }
        public string BackImage { get; private set; }
        public string FrontImage { get; private set; }
        public int ProductionYear { get; private set; }
        public float IMDBRating { get; set; } 
        public List<string> Genres { get; private set; }
        public List<string> Directors { get; private set; }
        public List<Actor> Actors { get; private set; }

        

        public string RunningTimeString
        {
            get
            {
                if (RunningTime > 0) return RunningTime.ToString();
                return "";
            }
        }

        public string ProductionYearString
        {
            get
            {
                if (ProductionYear > 1900) return ProductionYear.ToString();
                return "";
            }
        } 


        public Movie(string path)
        {
            Genres = new List<string>();
            Directors = new List<string>();
            Actors = new List<Actor>();

            try
            {
                LoadMetaData(path);
            }
            catch (Exception e)
            {
                Trace.WriteLine("bodgy metadata: " + path + " " + e.ToString());
                // bad metadata :( 
            }
        }

        public string DirectorString
        {
            get 
            {
                bool isFirst = true;
                StringBuilder sb = new StringBuilder();
                foreach (string director in Directors)
                {
                    if (!isFirst)
                    {
                        sb.Append(" and ");
                    }
                    else
                    {
                        isFirst = false;
                    }
                    sb.Append(director);
                }

                return sb.ToString();
            }
        }
        public string ActorsString
        {
            get
            {
                StringBuilder sb = new StringBuilder();
                foreach (Actor actor in Actors)
                {
                    sb.AppendLine(string.Format("{0} ... {1}", actor.Name, actor.Role)); 
                }

                return sb.ToString();
            }
        } 


        private void LoadMetaData(string path)
        {
            
      
            XmlDocument doc = new XmlDocument();
            doc.Load(path);

            Description = doc.SafeGetString("Title/Description");
            FrontImage = doc.SafeGetString("Title/Covers/Front");
            BackImage = doc.SafeGetString("Title/Covers/Front");
            RunningTime = doc.SafeGetInt("Title/RunningTime");
            ProductionYear = doc.SafeGetInt("Title/ProductionYear");
            IMDBRating = doc.SafeGetFloat("Title/IMDBrating", (float)0, (float)10); 

            foreach (XmlNode item in doc.SelectNodes("Title/Persons/Person"))
            {
                try
                {
                    XmlNode TypeNode = item.SelectSingleNode("Type");
                    if (TypeNode != null && TypeNode.InnerText != null)
                    {


                        switch (TypeNode.InnerText.ToLower())
                        {
                            case "actor":
                                Actors.Add(new Actor(item.SelectSingleNode("Name").InnerText, item.SelectSingleNode("Role").InnerText)); 
                                break;
                            case "director":
                                Directors.Add(item.SelectSingleNode("Name").InnerText);
                                break;
                        }
                    }
                }
                catch
                {
                    // fall through i dont care, one less actor/director
                } 
            }

            foreach (XmlNode item in doc.SelectNodes("Title/Genres/Genre"))
            {
                try
                {
                   Genres.Add(item.InnerText);
                }
                catch
                {
                    // fall through i dont care, one less actor/director
                } 
            }

        


        } 

    }
}
