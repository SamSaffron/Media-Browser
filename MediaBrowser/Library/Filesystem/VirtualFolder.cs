using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using MediaBrowser.Library.Filesystem;

namespace Configurator {

    public class VirtualFolder {

        VirtualFolderContents contents;

        string path;

        public VirtualFolder(string path) {
            this.path = path;
            contents = new VirtualFolderContents(File.ReadAllText(path));
        }

        public string Path { get { return path; } }

        public void RemoveFolder(string folder) {
            contents.RemoveFolder(folder);
            Save();
        }

        public void AddFolder(string folder) {
            contents.AddFolder(folder);
            Save();
        }

        public void Save() {
            File.WriteAllText(path, contents.Contents);
        }

        public List<string> Folders { get { return contents.Folders; } }

        public string ImagePath {
            get { return contents.ImagePath; }
            set { contents.ImagePath = value; Save(); }
        }

        public string Name {
            get { return System.IO.Path.GetFileNameWithoutExtension(path); }
            set {
                string newPath = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(path), value + ".vf");
                File.Move(path, newPath);
                path = newPath;
            }
        }

        public override string ToString() {
            return Name;
        }
    }
}
