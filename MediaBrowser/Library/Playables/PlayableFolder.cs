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
        List<string> videoFiles;
        PlayableExternal playableExternal = null;

        
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
                videoFiles = new List<string>(Helper.EnumerateVideoFiles(path,null,null,Config.Instance.EnableNestedMovieFolders));
                if (videoFiles.Count==1)
                {
                    playListFile = videoFiles[0];
                    if (PlayableExternal.CanPlay(playListFile))
                        this.playableExternal = new PlayableExternal(playListFile);
                }
                else
                {
                    videoFiles.Sort();
                    int pos = 0;
                    if (resume)
                        pos = PlayState.PlaylistPosition;

                    if (PlayableExternal.CanPlay(videoFiles[0]))
                    {
                        playListFile = Path.Combine(Helper.AutoPlaylistPath, Path.GetFileName(path) + ".pls");
                        StringBuilder contents = new StringBuilder("[playlist]\n");
                        int x = 1;
                        foreach (string file in videoFiles)
                        {
                            if (pos > 0)
                                pos--;
                            else
                            {
                                contents.Append("File" + x + "=" + file + "\n");
                                contents.Append("Title" + x + "=Part " + x + "\n\n");
                            }
                            x++;
                        }
                        contents.Append("Version=2\n");

                        System.IO.File.WriteAllText(playListFile, contents.ToString());
                        this.playableExternal = new PlayableExternal(playListFile);
                    }
                    else
                    {
                        playListFile = Path.Combine(Helper.AutoPlaylistPath, Path.GetFileName(path) + ".wpl");
                        StringBuilder contents = new StringBuilder(@"<?wpl version=""1.0""?><smil><body><seq>");
                        foreach (string file in videoFiles)
                        {
                            if (pos > 0)
                                pos--;
                            else
                            {
                                contents.Append(@"<media src=""");
                                contents.Append(file);
                                contents.AppendLine(@"""/>");
                            }
                        }
                        contents.Append(@"</seq></body></smil>");
                        System.IO.File.WriteAllText(playListFile, contents.ToString());
                    }
                }
            }
        }

        

        public override bool UpdatePosition(string title, long positionTicks)
        {
            if (title == null || videoFiles == null) 
                return false; 
            
            int i = 0;
            foreach (var filename in videoFiles)
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

        protected override void PlayInternal(bool resume)
        {
            if (this.playableExternal != null)
                this.playableExternal.Play(this.PlayState, resume);
            else
                base.PlayInternal(resume);
        }
    }
}
