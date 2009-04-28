using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using System.Threading;
using System.IO;
using MediaBrowser.Library.Filesystem;
using MediaBrowser.Library.Extensions;
using MediaBrowser.Library.Threading;

namespace TestMediaBrowser {
    [TestFixture]
    public class TestProtectedFileStream {

        /// <summary>
        /// Perform an action and notify when the action is done
        /// </summary>
        /// <param name="times">The amount of times the action is to be performed</param>
        /// <param name="asyncThreads">The number of threads perfomring the action (if 0 action is performed synchronisly)</param>
        /// <param name="action">The action to perform</param>
        /// <param name="done">The action to perform when all is done</param>
        private void Repeat(int times, int asyncThreads, Action action, Action done) {
            if (asyncThreads > 0) {

                var threads = new List<Thread>();

                for (int i = 0; i < asyncThreads; i++) {

                    int iterations = times / asyncThreads; 
                    if (i == 0) {
                        iterations += times % asyncThreads;                    
                    }

                    Thread thread = new Thread(new ThreadStart(() => Repeat(iterations, 0, action, null)));
                    thread.Start();
                    threads.Add(thread);
                }

                foreach (var thread in threads) {
                    thread.Join();
                }

            } else {
                for (int i = 0; i < times; i++) {
                    action();
                }
            }
            if (done != null) {
                done();
            }
        }

        private void Repeat(int times, bool async, Action action) {
            Repeat(times, async?1:0, action, null);
        }

        private void AssertAreEqual(byte[] array1, byte[] array2) {
            bool same = array1.Length == array2.Length;
            if (same) {
                for (int i = 0; i < array1.Length; i++) {
                    if (array1[i] != array2[i]) {
                        same = false;
                        break;
                    }
                }
            }

            Assert.IsTrue(same);
        }

        [Test]
        public void TestReaderAndWritersPlayNicely() {
            string file = Path.GetTempFileName();
            byte[] contents = new byte[] { 1, 2, 3 };
            File.WriteAllBytes(file, contents);


            for (int i = 0; i < 5; i++) {
                Repeat(5, true, () =>
                {
                    using (var stream = ProtectedFileStream.OpenSharedReader(file)) {
                        AssertAreEqual(contents, stream.ReadAllBytes());
                        Thread.Sleep(1);
                    }
                });
            }

            for (int i = 0; i < 5; i++) {
                Repeat(5, true, () =>
                {
                    using (var stream = ProtectedFileStream.OpenExclusiveWriter(file)) {
                        stream.Write(contents, 0, contents.Length);
                        Thread.Sleep(1);
                    }
                });
            }
            
        }

        [Test]
        public void TestSystemForgivesExternalBlockers() {
            string file = Path.GetTempFileName();
            byte[] contents = new byte[] { 1, 2, 3 };
            File.WriteAllBytes(file, contents);

            FileStream fs = File.Open(file, FileMode.Open, FileAccess.ReadWrite, FileShare.None);

            Repeat(1, true, () =>
            {
                using (var stream = ProtectedFileStream.OpenSharedReader(file)) {
                    AssertAreEqual(contents, stream.ReadAllBytes());
                }
            });

            Thread.Sleep(1);
            fs.Close();
        }

        [Test]
        public void TestReadersHaveConcurrentAccess() {
            string file = Path.GetTempFileName();
            byte[] contents = new byte[] {1,2,3};
            File.WriteAllBytes(file, contents);

            for (int i = 0; i < 5; i++) {
                Repeat(5, true, () =>
                {
                    using (var stream = ProtectedFileStream.OpenSharedReader(file)) {
                        AssertAreEqual(contents, stream.ReadAllBytes());
                    }
                });
            }
        }


        [Test]
        public void TestHeavyConcurrentAccessShouldNotCauseBlocking() {

            int fileCount = 10;

            int fileSize = 8096;

            Random rand = new Random();

            var files = Enumerable
                .Range(0, fileCount)
                .Select(_ => new KeyValuePair<string,byte[]> ( Path.GetTempFileName(),  GetRandomBytes(fileSize)))
                .ToList();


            foreach (var pair in files) {
                File.WriteAllBytes(pair.Key, pair.Value);
            }

            bool done = false;

            Repeat(500, 20, () => ReadOrWriteFromRandomFile(files), () => done = true);

            while (!done) {
                Thread.Sleep(1);
            }

        }

        void ReadOrWriteFromRandomFile(List<KeyValuePair<string, byte[]>> fileList) {

            var pair = fileList[new Random().Next(fileList.Count)];

            if (new Random().Next(2) == 1) {
                WriteBytesToFile(pair.Key, pair.Value);
            } else {
                var bytes = ReadBytesFromFile(pair.Key);
                Assert.IsTrue(Enumerable.SequenceEqual(bytes, pair.Value));
            }
        }

        byte[] GetRandomBytes(int count) {
            var bytes = new byte[count];
            (new Random()).NextBytes(bytes);
            return bytes;
        }
        

        private void WriteBytesToFile(string filename, byte[] bytes) {
            using (var stream = ProtectedFileStream.OpenExclusiveWriter(filename)) {
                stream.Write(bytes, 0, bytes.Length);
            }
        }

        private byte[] ReadBytesFromFile(string filename) {
            using (var stream = ProtectedFileStream.OpenSharedReader(filename)) {
                return stream.ReadAllBytes();
            }
        }
    }
}
