using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Windows.Forms;

namespace Configurator {
    // we really should be reusing this code. 
    public class VirtualFolder {

        List<string> folders = new List<string>();
        string image;

        string path;

        public VirtualFolder(string path) {
            this.path = path;
            foreach (var line in File.ReadAllLines(path)) {
                var colonPos = line.IndexOf(':');
                if (colonPos <= 0) {
                    continue;
                }

                var type = line.Substring(0, colonPos);
                var filename = line.Substring(colonPos + 1).Trim();

                if ((!File.Exists(filename) && type == "image") || (!Directory.Exists(filename) && type == "folder")) {
                    MessageBox.Show(string.Format("Ignoring invalid file {0} in virtual folder {1}", filename, path));
                    continue;
                }
                if (type == "image") {
                    image = filename;
                } else if (type == "folder") {
                    folders.Add(filename);
                }

            }
        }

        public string Path { get { return path; } }

        public void RemoveFolder(string folder) {
            folders.Remove(folder);
            Save();
        }

        public void AddFolder(string folder) {
            folders.Add(folder);
            Save();
        }

        public void Save() {

            StringBuilder contents = new StringBuilder();
            if (image != null && File.Exists(image)) {
                contents.AppendLine("image: " + image);
            }

            foreach (var folder in folders) {
                if (Directory.Exists(folder)) {
                    contents.AppendLine("folder: " + folder);
                }
            }

            File.WriteAllText(path, contents.ToString());
        }

        public List<string> Folders { get { return folders; } }

        public string ImagePath {
            get { return image; }
            set { image = value; Save(); }
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
