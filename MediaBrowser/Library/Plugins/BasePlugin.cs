using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MediaBrowser.Library.Plugins {

    /// <summary>
    /// This is the class plugins should inherit off, it implements gathering basic version info
    /// </summary>
    public abstract class BasePlugin : IPlugin {
        #region IPlugin Members

        public abstract void Init(Kernel kernel);

        public abstract string Name {
            get;
        }

        public abstract string Description {
            get;
        }

        /// <summary>
        /// Filename is assigned by the plugin discovery piece. 
        /// Do not override this.
        /// </summary>
        public string Filename {
            get {
                // if you instansiate a class inheriting off BasePlugin 
                // There is no way to tell what the plugin file name is.
                throw new NotSupportedException(); 
            } 
        } 

        public virtual System.Version Version {
            get {
                return this.GetType().Assembly.GetName().Version;
            }
        }

        /// <summary>
        /// Only override this if you want to control the logic for determining the latest version.
        /// </summary>
        public virtual System.Version LatestVersion {
            get;
            set;
        }

        #endregion
    }
}
