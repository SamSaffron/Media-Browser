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
    /// This provides information to the root page on-screen display. You have the option of adding
    /// one-time or recurring messages.
    /// </summary>
    public class Information : ModelItem
    {
        private const int CycleInterval = 4000;
        Timer cycle;
        int counter = 0;

        public Information()
        {
            AddInformation(new InfomationItem("Welcome to Media Browser.", false));
            Begin();
        }

        #region fields

        string _displayText = string.Empty;
        ArrayListDataSet _item = new ArrayListDataSet();

        public string DisplayText
        {
            get { return _displayText; }
            set { _displayText = value; FirePropertyChanged("DisplayText"); }
        }

        #endregion

        #region methods
        public void AddInformation(InfomationItem info)
        {
            _item.Add(info);
        }
        public void AddInformationString(string info)
        {
            _item.Add(new InfomationItem(info));
        }

        private void Begin()
        {
            if (_item.Count > 0)
                DisplayItem();

            cycle = new Timer(this);
            cycle.Interval = CycleInterval;
            cycle.Tick += delegate { OnRefresh(); };
            cycle.Enabled = true;
        }

        private void OnRefresh()
        {
            if (_item.Count > 0)
            {
                counter++;
                if (counter > (_item.Count - 1))
                    counter = 0;
                DisplayItem();
            }
            else
                DisplayText = string.Empty;
        }

        private void DisplayItem()
        {
            InfomationItem ipi = (InfomationItem)_item[counter];
            DisplayText = ipi.Description;
            // Check the Recurring flag, and remove this message once displayed
            if (!ipi.RecurrMessage)
                RemoveItem(counter);
        }

        private void RemoveItem(int index)
        {
            _item.RemoveAt(index);
            counter--;
        }

        #endregion
    }

    #region InformationItem Struct
    public struct InfomationItem
    {
        public string Description;
        public bool RecurrMessage;

        public InfomationItem(string description)
        {
            this.Description = description;
            this.RecurrMessage = false;
        }
        public InfomationItem(string description, bool recurrMessage)
        {
            this.Description = description;
            this.RecurrMessage = recurrMessage;
        }
    }
    #endregion
}
