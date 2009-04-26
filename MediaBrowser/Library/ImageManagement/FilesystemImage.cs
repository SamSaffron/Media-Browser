using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using MediaBrowser.Library.Filesystem;
using MediaBrowser.Library.Persistance;

namespace MediaBrowser.Library.ImageManagement {
    public class FilesystemImage : LibraryImage{

        [Persist]
        bool imageIsCached;
        bool isValid = false;

        public override void Init() {
            base.Init();

            imageIsCached = System.IO.Path.GetPathRoot(this.Path).ToLower() != System.IO.Path.GetPathRoot(cachePath).ToLower();
        }

        protected override string LocalFilename {
            get {
                if (!imageIsCached) return Path;
                return base.LocalFilename;
            }
        }

        public override string GetLocalImagePath() {
            if (!imageIsCached) return LocalFilename;

            lock (Lock) {
                if (!isValid && File.Exists(LocalFilename)) {
                    var localInfo = new System.IO.FileInfo(LocalFilename);
                    var remoteInfo = new System.IO.FileInfo(Path);
                    isValid = localInfo.LastWriteTimeUtc > remoteInfo.LastWriteTimeUtc;   
                }

                if (!isValid) {
                    byte[] data = File.ReadAllBytes(Path);
                    using (var stream = ProtectedFileStream.OpenExclusiveWriter(LocalFilename)) {
                        BinaryWriter bw = new BinaryWriter(stream);
                        bw.Write(data);
                    }
                    isValid = true;
                }
                
                return LocalFilename;
            }
        }
    
    }
}
