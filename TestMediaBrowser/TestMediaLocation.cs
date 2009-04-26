using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using System.IO;
using MediaBrowser.Library.Factories;
using MediaBrowser.Library.Filesystem;

namespace TestMediaBrowser {
    [TestFixture]
    public class TestMediaLocation {
        string testDir =  Path.Combine(Path.GetTempPath(), "MediaBrowserTests");
 
        [TearDown]
        public void Teardown() {
            if (Directory.Exists(testDir)) {
                Directory.Delete(testDir, true);
            }
        }

        [Test]
        public void TestStandardScanning() {
            CreateTree(3, 10, "hello world");
            var root = MediaLocationFactory.Instance.Create(testDir);
            Assert.AreEqual(3, (root as IFolderMediaLocation).Children.Count);

            foreach (var item in (root as IFolderMediaLocation).Children) {
                Assert.AreEqual(10, (item as FolderMediaLocation).Children.Count);
            }
        }


        public void CreateTree(int subDirs, int filesPerSubdir, string fileContents) {
            var info = Directory.CreateDirectory(testDir);

            for (int i = 0; i < subDirs; i++) {
                var sub = Directory.CreateDirectory(Path.Combine(info.FullName, "SubDir" + i.ToString()));

                for (int j = 0; j < filesPerSubdir; j++) {
                    File.WriteAllText(Path.Combine(sub.FullName,"File" + j.ToString()), fileContents);
                }
            }

        }
    }
}
