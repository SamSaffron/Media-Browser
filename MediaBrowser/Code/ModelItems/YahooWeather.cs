using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using System.Net;
using System.IO;
using System.Xml.XPath;
using Microsoft.MediaCenter.UI;
using System.Diagnostics;

namespace MediaBrowser
{
    /// <summary>
    /// This model item uses the Yahoo! Developer API to retrieve the weather.
    /// Full details can be found here: http://developer.yahoo.com/weather/
    /// </summary>
    public class YahooWeather : ModelItem
    {
        #region static fields 
        private static readonly string FileName = string.Format("weather_{1}_{0}.xml", Application.CurrentInstance.Config.YahooWeatherFeed, Application.CurrentInstance.Config.YahooWeatherUnit);
        private readonly string DownloadToFilePath = Path.Combine(LibraryManagement.Helper.AppRSSPath, FileName);        
        private readonly string Feed = string.Format("http://weather.yahooapis.com/forecastrss?p={0}&u={1}",
            Application.CurrentInstance.Config.YahooWeatherFeed,
            Application.CurrentInstance.Config.YahooWeatherUnit);
        private const int RefreshIntervalHrs = 3;

        #endregion

        public YahooWeather()
        {
           // GetWeatherInfo();
        }

        #region fields
        string _imageUrl = "";
        string _code = "";
        string _codeDescription = "";
        string _location = "";
        string _temp = "";
        string _unit = "";
        ArrayListDataSet _forecast = new ArrayListDataSet();

        public string Code
        {
            get { return _code; }
            set { _code = value; FirePropertyChanged("Code"); }
        }

        public string CodeDescription
        {
            get { return _codeDescription; }
            set { _codeDescription = value; FirePropertyChanged("CodeDescription"); }
        }

        public string ImageUrl
        {
            get { return _imageUrl; }
            set { _imageUrl = value; FirePropertyChanged("ImageUrl"); }
        }


        public string Location
        {
            get { return _location; }
            set { _location = value; FirePropertyChanged("Location"); }
        }

        public string Temp
        {
            get { return _temp; }
            set { _temp = value; FirePropertyChanged("Temp"); }
        }

        public string Unit
        {
            get { return _unit; }
            set { _unit = value; FirePropertyChanged("Unit"); }
        }
        public ArrayListDataSet Forecast
        {
            get { return _forecast; }
        }
        #endregion

        #region methods
        
        public void GetWeatherInfo()
        {
            WebClient client = new WebClient();
            XmlDocument xDoc = new XmlDocument(); 
            try
            {
                if (IsRefreshRequired())
                {
                    client.DownloadFile(Feed, DownloadToFilePath);
                    Stream strm = client.OpenRead(Feed);                    
                    StreamReader sr = new StreamReader(strm);
                    string strXml = sr.ReadToEnd();
                    xDoc.LoadXml(strXml); 
                }
                else
                {
                    xDoc.Load(DownloadToFilePath); 
                }                             
                               
                ParseYahooWeatherDoc(xDoc);
            }
            catch (Exception e)
            {
                Application.Logger.ReportException("Yahoo weather refresh failed" , e);
            }
            finally
            {
                client.Dispose();
            }
        }

        private bool IsRefreshRequired()
        {
            if (File.Exists(DownloadToFilePath))
            {
                FileInfo fi = new FileInfo(DownloadToFilePath);
                if (fi.LastWriteTime < DateTime.Now.AddHours(-(RefreshIntervalHrs)))
                    return true;
                else
                    return false;
            }
            // If we get to this stage that means the file does not exists, and we should force a refresh
            return true;
        }

        private void ParseYahooWeatherDoc(XmlDocument xDoc)
        {
            //Setting up NSManager
            XmlNamespaceManager man = new XmlNamespaceManager(xDoc.NameTable);
            man.AddNamespace("yweather", "http://xml.weather.yahoo.com/ns/rss/1.0");

            this.Unit = xDoc.SelectSingleNode("rss/channel/yweather:units", man).Attributes["temperature"].Value.ToString();
            this.CodeDescription = xDoc.SelectSingleNode("rss/channel/item/yweather:condition", man).Attributes["text"].Value.ToString();
            this.Location = xDoc.SelectSingleNode("rss/channel/yweather:location", man).Attributes["city"].Value.ToString();
            this.Temp = xDoc.SelectSingleNode("rss/channel/item/yweather:condition", man).Attributes["temp"].Value.ToString();
            this.Code = xDoc.SelectSingleNode("rss/channel/item/yweather:condition", man).Attributes["code"].Value.ToString();
            this.ImageUrl = string.Format("resx://MediaBrowser/MediaBrowser.Resources/_{0}", this.Code);
            //this.ImageUrl = string.Format("http://l.yimg.com/a/i/us/we/52/{0}.gif", this.Code);

            var tempForecast = xDoc.SelectNodes("rss/channel/item/yweather:forecast", man);
            //<yweather:forecast day="Fri" date="24 Apr 2009" low="50" high="63" text="Partly Cloudy" code="30" />
            foreach (XmlNode temp in tempForecast)
            {
                ForecastItem fi = new ForecastItem();
                fi.Day = temp.Attributes["day"].Value.ToString();
                fi.Date = temp.Attributes["date"].Value.ToString();
                fi.Low = temp.Attributes["low"].Value.ToString();
                fi.High = temp.Attributes["high"].Value.ToString();
                fi.Code = temp.Attributes["code"].Value.ToString();
                fi.CodeDescription = temp.Attributes["text"].Value.ToString();
                fi.ImageUrl = string.Format("resx://MediaBrowser/MediaBrowser.Resources/_{0}", fi.Code);
                _forecast.Add(fi);
            }

        }

        #endregion
    }

    public class ForecastItem : ModelItem
    {
        public ForecastItem()
        {
        }

        #region fields
        string _imageUrl, _code , _codeDescription, _day, _date, _low, _high = string.Empty;

        public string Code
        {
            get { return _code; }
            set { _code = value; FirePropertyChanged("Code"); }
        }

        public string CodeDescription
        {
            get { return _codeDescription; }
            set { _codeDescription = value; FirePropertyChanged("CodeDescription"); }
        }

        public string ImageUrl
        {
            get { return _imageUrl; }
            set { _imageUrl = value; FirePropertyChanged("ImageUrl"); }
        }


        public string Day
        {
            get { return _day; }
            set { _day = value; FirePropertyChanged("Day"); }
        }

        public string Date
        {
            get { return _date; }
            set { _date = value; FirePropertyChanged("Date"); }
        }

        public string Low
        {
            get { return _low; }
            set { _low = value; FirePropertyChanged("Low"); }
        }
        public string High
        {
            get { return _high; }
            set { _high = value; FirePropertyChanged("High"); }
        }
        #endregion

    }
}
