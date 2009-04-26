using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MediaBrowser.Library.Providers;
using MediaBrowser.Library.Entities;
using MediaBrowser.Library.Providers.Attributes;
using MediaBrowser.Library;
using Toub.MediaCenter.Dvrms.Metadata;
using System.Collections;
using System.Diagnostics;
using System.IO;
using MediaBrowser.LibraryManagement;
using System.Drawing.Imaging;
using MediaBrowser.Library.Persistance;

namespace DvrmsMetadataProvider {

    static class DvrmsMetadataEditorHelpers {

        public static T GetValue<T>(this IDictionary dict, string lookup) where T : class  {
            T val = default(T);
            var metaItem = dict[lookup] as MetadataItem;
            if (metaItem != null) {
                val = metaItem.Value as T;
            }
            return val;
        }
    }

    [SupportedType(typeof(Video),SubclassBehavior.Include)]
    public class DvrmsMetadataProvider : BaseMetadataProvider{

        public Show Show { get { return (Show)Item; } }
        public bool IsDvrms { get { return Item.Path.ToLower().EndsWith(".dvr-ms"); } }

        [Persist]
        DateTime updateDate = DateTime.MinValue;

        public override void Fetch() {
            if (!IsDvrms) return;
            
            bool success = false; 
            int attempts = 5;
            while (attempts > 0 && !success) {
                try {
                    UpdateMetadata();
                    success = true;
                    attempts--; 
                } catch (Exception ex) {
                    Trace.WriteLine("Failed to get metadata: retrying " + ex.ToString());
                }
            }
            updateDate = DateTime.Now;
            
        }

        private void UpdateMetadata() {
            using (DvrmsMetadataEditor editor = new DvrmsMetadataEditor(Item.Path)) {
                var attribs = editor.GetAttributes();

                string name = attribs.GetValue<string>(MetadataEditor.Title);

                // australia ice tv adds MOVIE in front of movies
                if (name != null && name.StartsWith("MOVIE: ")) {
                    name = name.Substring(7);
                }

                string subtitle = attribs.GetValue<string>(MetadataEditor.Subtitle);
                string overview = attribs.GetValue<string>(MetadataEditor.SubtitleDescription);

                Item.Name = name;
                Item.SubTitle = subtitle;
                Item.Overview = overview;

                var image = editor.GetWMPicture();

                if (image != null) {
                    lock (typeof(BaseMetadataProvider)) {
                        var imagePath = Path.Combine(Helper.AppImagePath, Item.Id.ToString() + ".png");
                        image.Picture.Save(imagePath, ImageFormat.Png);
                        Item.PrimaryImagePath = imagePath;
                    }
                }
            }
        }

        private static void OutputDiagnostics(IDictionary attribs) {
            foreach (var key in attribs.Keys) {
                var item = attribs[key] as MetadataItem;
                Trace.WriteLine(key.ToString() + " " + item.Value.ToString());
            }
            Trace.WriteLine("");
        }

        public override bool NeedsRefresh() {
            return IsDvrms && new FileInfo(Item.Path).LastWriteTime > updateDate;
        }
        
    }
}
