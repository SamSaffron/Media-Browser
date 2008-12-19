using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using MediaBrowser.LibraryManagement;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace MediaBrowser.Library.Playables
{
    class PlayableFolder : PlayableItem
    {
        string path;
        string playListFile;
        string[] filenames;
        int offset = 0;

        public PlayableFolder(string path)
            : base()
        {
            this.path = path;
        }

        public override void Prepare(bool resume)
        {
            if (Helper.IsDvDFolder(path,null,null))
                playListFile = path;
            else
            {
                filenames = Directory.GetFiles(path);

                if (filenames != null)
                {
                    List<string> videoFiles = new List<string>();
                    foreach (string f in filenames)
                        if (Helper.IsVideo(f))
                            videoFiles.Add(f);
                    videoFiles.Sort();

                    if (resume)
                    {
                        offset = PlayState.PlaylistPosition;
                        int pos = offset;
                        while (pos > 0)
                        {
                            if (videoFiles.Count > 0)
                            {
                                videoFiles.RemoveAt(0);
                            }
                            pos--;
                        }
                    }

                    filenames = videoFiles.ToArray();
                    playListFile = Path.Combine(Helper.AutoPlaylistPath, Path.GetFileName(path) + ".wpl");
                    StringBuilder contents = new StringBuilder(@"<?wpl version=""1.0""?><smil><body><seq>");
                    foreach (string file in videoFiles)
                    {
                        contents.Append(@"<media src=""");
                        contents.Append(file);
                        contents.AppendLine(@"""/>");
                    }
                    contents.Append(@"</seq></body></smil>");
                    System.IO.File.WriteAllText(playListFile, contents.ToString());
                }
            }
        }

        public override void Play(PlayState playstate, bool resume)
        {
            base.Play(playstate, resume);
        }


        public override bool UpdatePosition(string title, long positionTicks)
        {
            if (title == null || filenames == null) 
                return false; 
            
            int i = offset;
            foreach (var filename in filenames)
	        {
                if (title.StartsWith(Path.GetFileNameWithoutExtension(filename)))
                {
                    PlayState.PlaylistPosition = i;
                    PlayState.PositionTicks = positionTicks;
                    return true;
                }
                i++;
	        }

            return false;
        }

        public override string Filename
        {
            get { return playListFile; }
        }

        public static bool CanPlay(string path)
        {
            if (!Helper.IsFolder(path))
                return false;
            foreach (string file in Helper.EnumerateVideoFiles(path,null,null,true))
                return true;
            return false;
        }
    }
}
