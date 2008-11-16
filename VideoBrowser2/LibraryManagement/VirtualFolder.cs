using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Text.RegularExpressions; 

namespace SamSoft.VideoBrowser.LibraryManagement
{
    public class VirtualFolder
    {
        public List<string> Folders = new List<string>();
        public string ThumbPath;
        public string Path; 

        public VirtualFolder(string path)
        {
			Path = path;
            foreach (var line in File.ReadAllLines(path))
            {
                if (line.StartsWith("image:"))
                {
                    // TODO: test if it is a valid image
					string thumbPath = line.Substring(6).Trim();
                    if ((thumbPath.StartsWith(@".\")) || (thumbPath.StartsWith(@"..\")))
                    {
                        thumbPath = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(path), thumbPath);
                        thumbPath = System.IO.Path.GetFullPath(thumbPath);
                    }
					if (!IsValidPath(thumbPath) || !File.Exists(thumbPath))
					{
						Application.DialogBoxViaReflection("Invalid virtual folder thumbnail path: " + thumbPath);
					}
					else
					{
						ThumbPath = thumbPath;
					}
                }
                else if (line.StartsWith("folder:"))
                { 
					string folderPath = line.Substring(7).Trim();
                    if ((folderPath.StartsWith(@".\")) || (folderPath.StartsWith(@"..\")))
                    {
                        folderPath = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(path), folderPath);
                        folderPath = System.IO.Path.GetFullPath(folderPath);
                    }

					if (!IsValidPath(folderPath) || !Directory.Exists(folderPath))
					{
						Application.DialogBoxViaReflection("Invalid virtual folder path: " + folderPath);
					}
					else
					{
						Folders.Add(folderPath);
					}
                }
            }
        }

		/// <summary>
		/// Gets whether the specified path is a valid absolute file path.
		/// http://www.csharp411.com/check-valid-file-path-in-c/
		/// </summary>
		/// <param name="path">Any path. OK if null or empty.</param>
		static public bool IsValidPath(string path)
		{
			Regex r = new Regex(@"^(([a-zA-Z]\:)|(\\))(\\{1}|((\\{1})[^\\]([^/:*?<>""|]*))+)$");
			return r.IsMatch(path);
		}

    }
}
