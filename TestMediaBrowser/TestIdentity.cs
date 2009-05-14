using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MediaBrowser;
using MediaBrowser.Library.Factories;
using NUnit.Framework;
using MediaBrowser.Library.Entities;
using MediaBrowser.Library.Filesystem;
using MediaBrowser.Library;
using System.IO;
using System.Security.Cryptography;
using System.Diagnostics;

namespace TestMediaBrowser {

    static class LinqExtension {
        public static IEnumerable<T> SampleEvery<T>(this IEnumerable<T> items, int sample) {
            int i = 0; 
            foreach (var item in items) {
                if ((i % sample) == 0) {
                    yield return item;
                }
                i++;
            }
        }
    }

    [TestFixture]
    public class TestIdentity {

        // this test is just to help me determine a good identity algorithm 
        
        [Ignore("Takes too long")]
        [Test]
        public void TestIdentityPerformance() { 
            
            
            /*
            var root = (AggregateFolder)BaseItemFactory.Instance.Create(rootLocation);

            var videos = root.RecursiveChildren
                .Where(i => i is Video && (i as Video).MediaType != MediaType.DVD)
                .Select(i => (i as Video).VideoFiles)
                .SelectMany(_ => _)
                .Where(filename => !filename.StartsWith("http"))
                .Take(200)
                .ToArray();
             */

            var rootLocation = Kernel.Instance.GetLocation<IFolderMediaLocation>(@"c:\users");

            var files = Recurse(rootLocation)
                .Where(location => !(location is IFolderMediaLocation))
                .Select(location => location.Path)
                .ToArray();

            Console.WriteLine(files.Length);

            int done = 0;

            Stopwatch watch = new Stopwatch();
            watch.Start();
            Dictionary<string, Guid> fileHashs = new Dictionary<string, Guid>();
            foreach (var file in files) {
                try {
                    fileHashs[file] = GetFileSignature(file);
                } catch { 
                    // skip this file its probably locked
                }
                done++;
                if (done % 200 == 0) {
                    watch.Stop();
                    Console.WriteLine("current rate is : {0} items per second! {1} items done",(1000 / (float)watch.ElapsedMilliseconds) * 200, done );
                    watch.Reset();
                    watch.Start();
                }
            }


            foreach (var group in
                        fileHashs.GroupBy(pair => pair.Value)
                            .Where(group => group.Count() > 1)) {

                Console.WriteLine("Found Duplicate Files: ");

                var md5Provider = new MD5CryptoServiceProvider();
                Guid? oldHash = null; 
                foreach (var item in group) {
                    Console.WriteLine(item.Key);

                    var hash = new Guid(md5Provider.ComputeHash(File.ReadAllBytes(item.Key)));
                    if (oldHash != null) {
                        if (hash != oldHash) {
                            Console.WriteLine("ALGORITHM FAILURE");
                        }
                    }
                    oldHash = hash;
                }

                Console.WriteLine("");
            }


            Console.WriteLine(fileHashs.Count());
            Console.WriteLine(fileHashs.Values.Distinct().Count());

            // now all children are loaded we can do some benchmarking
        }

        public IEnumerable<IMediaLocation> Recurse(IMediaLocation location) {
            var folder = location as IFolderMediaLocation;
            if (folder != null) {
                foreach (var child in folder.Children) {
                    foreach (var decendant in Recurse(child)) {
                        yield return decendant;
                    }
                    yield return child;
                }
            }
            yield return location;
        }

        // number of samples to take
        const int SampleCount = 4; 

        // size of each random file sample
        const int SampleSize = 4 * 1024;
        
        // files smaller than this get no random sampling 
        const int SamplingThreshold = 16 * 1024;

        public static Guid GetFileSignature(string filename) {

            byte[] buffer;
            long filesize; 

            using (var reader = File.Open(filename, FileMode.Open, FileAccess.Read)) {

                filesize = reader.Length;

                if (filesize < SamplingThreshold) {
                
                    buffer = new byte[filesize];
                    Read(reader, buffer, 0, (int)filesize);
                
                } else {

                    Random random = new Random((int)(filesize % int.MaxValue));

                    int maxSize = filesize < (long)Int32.MaxValue ? (int)filesize : Int32.MaxValue;

                    // space out random numbers
                    var startPositions = Enumerable
                        .Range(0, SampleCount * 4)
                        .Select(_ => random.Next(maxSize))
                        .OrderBy(i => i)
                        .SampleEvery(4)
                        .ToArray();

                    buffer = new byte[SampleCount * SampleSize];
                    int bufferPosition = 0;

                    long currentPosition = 0;

                    foreach (var start in startPositions) {
                        currentPosition = reader.Seek(start - currentPosition, SeekOrigin.Current);
                        var bytesRead = Read(reader, buffer, bufferPosition, SampleSize);
                        currentPosition += bytesRead;
                        bufferPosition += bytesRead;
                    }
                }
            }
            var md5Provider = new MD5CryptoServiceProvider();
            md5Provider.TransformBlock(buffer, 0, buffer.Length, buffer, 0);
            // include the filesize in the hash
            var fileSizeArray = BitConverter.GetBytes(filesize);
            md5Provider.TransformFinalBlock(fileSizeArray, 0, fileSizeArray.Length);

            return new Guid(md5Provider.Hash);
        }

        private static int Read(FileStream reader, byte[] buffer, int offset, int count) {
            int totalBytesRead = 0;
            int bytesRead = 0;
            do {
                bytesRead = reader.Read(buffer, offset + totalBytesRead, count - totalBytesRead);
                totalBytesRead += bytesRead;
            } while (totalBytesRead < count && bytesRead > 0);
            
            return totalBytesRead;
        }
    }
}
