using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;
using Microsoft.MediaCenter.Hosting;
using Microsoft.MediaCenter;
using MediaBrowser.LibraryManagement;
using System.IO;

namespace MediaBrowser.Library.Playables
{
    class PlayableIso : PlayableItem
    {
        string file;
        string mountedFilename;
        PlayableExternal playableExternal = null;

        public PlayableIso(string file)
            : base()
        {
            if ((new FileInfo(file).Attributes & FileAttributes.Directory) == FileAttributes.Directory)
            {
                List<string> files = Helper.GetIsoFiles(file);
                if (files.Count > 0)
                    this.file = files[0];
                else
                    throw new NotSupportedException(file + " does not contain any iso files");
            }
            else
                this.file = file;
        }

        public override void Prepare(bool resume)
        {
            try
            {
                // Create the process start information.
                Process process = new Process();
                if (Config.Instance.DaemonToolsLocation.ToLower().EndsWith("vcdmount.exe"))
                    process.StartInfo.Arguments = "-mount \"" + this.file + "\"";
                else
                    process.StartInfo.Arguments = "-mount 0,\"" + this.file + "\"";
                process.StartInfo.FileName = Config.Instance.DaemonToolsLocation;
                process.StartInfo.ErrorDialog = false;
                process.StartInfo.CreateNoWindow = true;

                // We wait for exit to ensure the iso is completely loaded.
                process.Start();
                process.WaitForExit();

                // Play the DVD video that was mounted.
                this.mountedFilename = Config.Instance.DaemonToolsDrive + ":\\";
                if (PlayableExternal.CanPlay(this.mountedFilename))
                    this.playableExternal = new PlayableExternal(this.mountedFilename);
            }
            catch (Exception)
            {
                // Display the error in this case, they might wonder why it didn't work.
                AddInHost.Current.MediaCenterEnvironment.Dialog("DaemonTools is not correctly configured.", "Could not load ISO", DialogButtons.Ok, 10, true);
                throw (new Exception("Daemon tools is not configured correctly"));
            }
        }

        public override string Filename
        {
            get { return this.mountedFilename; }
        }

        public static bool CanPlay(string path)
        {
            if (Helper.IsIso(path))
                return true;
            if (Helper.IsoCount(path,null) == 1)
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
