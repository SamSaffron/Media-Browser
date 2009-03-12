using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Reflection;
using System.IO;
using System.Diagnostics;
using Microsoft.Win32;


namespace Bootstrapper {
    public class App : Application{

        /// <summary>
        /// Application Entry Point.
        /// </summary>
        [System.STAThreadAttribute()]
        public static void Main() {

            if (IsDotNet35Installed())
                LaunchInstaller();
            else {

                var app = new Bootstrapper.App();
                var main = new Main();
                main.Show();
                app.Run(main);
            }
        }

        public static bool IsDotNet35Installed() {
            RegistryKey key = Registry.LocalMachine;
            using (key = key.OpenSubKey(@"Software\Microsoft\NET Framework Setup\NDP\v3.5\1033"))
            {
            	return (key != null);
            }
        }

        public static void LaunchInstaller()
        {
            var installer = ExtractInstaller(); 
            Process p = Process.Start("msiexec.exe",  "/i \"" + installer + "\"");
            p.WaitForExit();
        }

        public static string ExtractInstaller() {
            var tempfile = Path.Combine(Path.GetTempPath(), "MediaBrowser.msi"); ;
            var name = "Bootstrapper.MediaBrowser.msi";
            using (var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(name)) {
                File.WriteAllBytes(tempfile, ReadStream(stream,1024*1000));
            }
            return tempfile;
        }


        // from http://www.yoda.arachsys.com/csharp/readbinary.html
        private static byte[] ReadStream(Stream stream, int initialLength) {

            // If we've been passed an unhelpful initial length, just
            // use 32K.
            if (initialLength < 1) {
                initialLength = 32768;
            }

            byte[] buffer = new byte[initialLength];
            int read = 0;

            int chunk;
            while ((chunk = stream.Read(buffer, read, buffer.Length - read)) > 0) {
                read += chunk;

                // If we've reached the end of our buffer, check to see if there's
                // any more information
                if (read == buffer.Length) {
                    int nextByte = stream.ReadByte();

                    // End of stream? If so, we're done
                    if (nextByte == -1) {
                        return buffer;
                    }

                    // Nope. Resize the buffer, put in the byte we've just
                    // read, and continue
                    byte[] newBuffer = new byte[buffer.Length * 2];
                    Array.Copy(buffer, newBuffer, buffer.Length);
                    newBuffer[read] = (byte)nextByte;
                    buffer = newBuffer;
                    read++;
                }
            }
            // Buffer is now too big. Shrink it.
            byte[] ret = new byte[read];
            Array.Copy(buffer, ret, read);
            return ret;
        }
    }
}
