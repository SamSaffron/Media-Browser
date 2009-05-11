using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Reflection;
using System.IO.Compression;
using System.Runtime.InteropServices;

namespace MediaInfoProvider {

    public class LibraryLoader {

        [DllImport("kernel32")]
        static extern IntPtr LoadLibrary(string lpFileName);

        public static void Extract(string name, string target) {
            using (var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(name)) {
                GZipStream unzippedStream = new GZipStream(stream, CompressionMode.Decompress, true);

                try {
                    File.WriteAllBytes(target, ReadStream(unzippedStream, 0));
                } catch {
                    // nothing we can do, just try to load whats there
                }
            }
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
