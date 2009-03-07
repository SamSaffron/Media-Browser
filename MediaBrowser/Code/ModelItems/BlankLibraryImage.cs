using System;
using Microsoft.MediaCenter.UI;

namespace MediaBrowser.Library {
    public class BlankLibraryImage : LibraryImage {
        public static BlankLibraryImage Instance = new BlankLibraryImage();

        public BlankLibraryImage()
            : base(null) {
        }


        public override Image Image {
            get {
                return null;
            }
        }

        public override Image SmallImage {
            get {
                return null;
            }
        }

        public override ImageSource Source {
            get {
                return null;
            }
            set {

            }
        }
    }
}
