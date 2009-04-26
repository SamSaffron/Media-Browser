using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using System.Threading;
using System.IO;
using MediaBrowser.Library.Filesystem;
using MediaBrowser.Library.Extensions;

namespace TestMediaBrowser {
    [TestFixture]
    public class TestProtectedFileStream {

        private void Repeat(int times, bool async, Action action) {
            if (async) {
                Thread thread = new Thread(new ThreadStart(() => Repeat(times, false, action)));
                thread.Start();
            } else {
                for (int i = 0; i < times; i++) {
                    action();
                }
            }
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
    }
}
