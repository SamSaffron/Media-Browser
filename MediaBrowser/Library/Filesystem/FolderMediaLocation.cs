using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using MediaBrowser.Library.Util;
using MediaBrowser.LibraryManagement;
using MediaBrowser.Library.Extensions;
using MediaBrowser.Library.Interop;

namespace MediaBrowser.Library.Filesystem {
    public class FolderMediaLocation : MediaLocation, IFolderMediaLocation  {


        internal FolderMediaLocation(FileInfo info, IFolderMediaLocation parent)
            : this(info, parent, null) 
        {
        }

        protected override void SetName() {
            Name = System.IO.Path.GetFileName(Path);
        }

        // special constructor used by the virtual folders (allows for folder relocation)
        internal FolderMediaLocation(FileInfo info, IFolderMediaLocation parent, IFolderMediaLocation location)
            : base(info, parent) {
            children = new Lazy<IList<IMediaLocation>>(GetChildren);
            if (location == null) {
                this.location = this;
            } else {
                this.location = location;
            }
        }


        public IList<IMediaLocation> Children {
            get {
                return children.Value;
            }
        }

        #region private

        private IFolderMediaLocation location; 
        Lazy<IList<IMediaLocation>> children;

        private IList<IMediaLocation> GetChildren() {
            var children = new List<IMediaLocation>();

            foreach (var file in GetFileInfos(Path)) {

                FileInfo resolved = file; 

                if (file.Path.IsShortcut()) {
                    var resolvedPath = Helper.ResolveShortcut(file.Path);
                    if (File.Exists(resolvedPath)) {
                        resolved = new System.IO.FileInfo(resolvedPath).ToFileInfo();
                    } else if (Directory.Exists(resolvedPath)) {
                        resolved = new System.IO.DirectoryInfo(resolvedPath).ToFileInfo();
                    } else {
                        continue;
                    }
                }

                if (resolved.Path.IsVirtualFolder()) {
                    children.Add(new VirtualFolderMediaLocation(resolved, location)); 
                }  
                else {
                    if (resolved.IsDirectory) {
                        children.Add(new FolderMediaLocation(resolved, location));
                    } else {
                        children.Add(new MediaLocation(resolved, location));
                    }
                }
            }

            return children;
        }

        #endregion 
    
        static List<FileInfo> GetFileInfos(string directory) {
            IntPtr INVALID_HANDLE_VALUE = new IntPtr(-1);
            FindFileApis.WIN32_FIND_DATAW findData;
            IntPtr findHandle = INVALID_HANDLE_VALUE;

            var info = new List<FileInfo>();
            try {
                findHandle = FindFileApis.FindFirstFileW(directory + @"\*", out findData);
                if (findHandle != INVALID_HANDLE_VALUE) {

                    do {
                        if (findData.cFileName == "." || findData.cFileName == "..") continue;

                        string fullpath = directory + (directory.EndsWith(@"\") ? "" : @"\") +
                              findData.cFileName;

                        bool isDir = false;

                        if ((findData.dwFileAttributes & FileAttributes.Directory) != 0) {
                            isDir = true;
                        }

                        info.Add(new FileInfo()
                        {
                            DateCreated = findData.ftCreationTime.ToDateTime(),
                            DateModified = findData.ftLastWriteTime.ToDateTime(),
                            IsDirectory = isDir,
                            Path = fullpath
                        });
                    }
                    while (FindFileApis.FindNextFile(findHandle, out findData));

                }
            } finally {
                if (findHandle != INVALID_HANDLE_VALUE) FindFileApis.FindClose(findHandle);
            }
            return info;
        }

    }
}
