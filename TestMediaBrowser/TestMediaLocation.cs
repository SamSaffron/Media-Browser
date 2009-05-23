using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using System.IO;
using MediaBrowser.Library.Factories;
using MediaBrowser.Library.Filesystem;
using MediaBrowser.Library;

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


        // note this test can take a while.. 
        [Test]
        public void DodgyVfsShouldPartiallyLoad() {

            var vf = Path.Combine(testDir, "test.vf");

            Directory.CreateDirectory(testDir);
            var dir1 = Path.Combine(testDir, "test");
            Directory.CreateDirectory(dir1 + "\\path");

            VirtualFolderContents generator = new VirtualFolderContents("");
            generator.AddFolder(dir1);
            generator.AddFolder(@"\\10.0.0.4\mydir");
           
            File.WriteAllText(vf, generator.Contents);

            var root = Kernel.Instance.GetLocation<VirtualFolderMediaLocation>(vf) ;

            Assert.AreEqual(1, root.Children.Count);
            
        }

        [Test]
        public void VirtualFoldersCanContainDuplicateFiles() {
            Directory.CreateDirectory(testDir);

            var dir1 = Path.Combine(testDir, "test");
            var dir2 = Path.Combine(testDir, "test2");

            var vf = Path.Combine(testDir, "test.vf");

            Directory.CreateDirectory(dir1 + "\\path");
            Directory.CreateDirectory(dir2 + "\\path");


            VirtualFolderContents generator = new VirtualFolderContents("");
            generator.AddFolder(dir1);
            generator.AddFolder(dir2);
            
            File.WriteAllText(vf, generator.Contents);

            var root = Kernel.Instance.GetLocation<VirtualFolderMediaLocation>(vf);
            
            Assert.AreEqual(2, root.Children.Count);
            Assert.AreEqual(true, root.ContainsChild("path"));
        } 

        [Test]
        public void TestStandardScanning() {
            CreateTree(3, 10, "hello world");
            var root = Kernel.Instance.GetLocation(testDir);
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
