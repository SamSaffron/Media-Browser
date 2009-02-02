using System;
using System.Collections.Generic;
using System.Text;
using MediaBrowser.LibraryManagement;
using System.IO;

namespace MediaBrowser.Library.Providers
{
    class ImageByNameProvider : ImageFromMediaLocationProvider
    {
        

        public override ItemType SupportedTypes
        {
            get { return ItemType.All; }
        }

        protected override string ProviderName
        {
            get
            {
                return "ImageByName";
            }
        }

        protected override string GetLocation(Item item)
        {
            string location = Config.Instance.ImageByNameLocation;
            if ((location == null) || (location.Length == 0))
                location = Path.Combine(Helper.AppDataPath, "ImagesByName");
            char[] invalid = Path.GetInvalidFileNameChars();
            string name = item.Source.Name;
            foreach (char c in invalid)
                name = name.Replace(c.ToString(), "");
            return Path.Combine(location, name);
        } 
    }
}
