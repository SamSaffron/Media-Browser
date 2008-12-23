using System;
using System.Text;
using System.Collections.Generic;

using Microsoft.VisualStudio.TestTools.UnitTesting;
using MediaBrowser.Library.Providers;
using MediaBrowser.Library.Sources;
using MediaBrowser.Library;
using System.Diagnostics;
using System.Reflection;
using System.IO;
using MediaBrowser.LibraryManagement;

namespace TestMediaBrowser
{
    /// <summary>
    /// Summary description for TestTheMovieDB
    /// </summary>
    [TestClass]
    public class TestTheMovieDB
    {
        public TestTheMovieDB()
        {
            //
            // TODO: Add constructor logic here
            //
        }
        
        [TestInitialize]
        public void Setup()
        {
            typeof(Microsoft.MediaCenter.UI.Application).GetMethod("RegisterUIThread", BindingFlags.NonPublic | BindingFlags.Static).Invoke(null, null);
        }

        private TestContext testContextInstance;

        /// <summary>
        ///Gets or sets the test context which provides
        ///information about and functionality for the current test run.
        ///</summary>
        public TestContext TestContext
        {
            get
            {
                return testContextInstance;
            }
            set
            {
                testContextInstance = value;
            }
        }

        #region Additional test attributes
        //
        // You can use the following additional attributes as you write your tests:
        //
        // Use ClassInitialize to run code before running the first test in the class
        // [ClassInitialize()]
        // public static void MyClassInitialize(TestContext testContext) { }
        //
        // Use ClassCleanup to run code after all tests in a class have run
        // [ClassCleanup()]
        // public static void MyClassCleanup() { }
        //
        // Use TestInitialize to run code before running each test 
        // [TestInitialize()]
        // public void MyTestInitialize() { }
        //
        // Use TestCleanup to run code after each test has run
        // [TestCleanup()]
        // public void MyTestCleanup() { }
        //
        #endregion

        private void Compare(string str1, string str2)
        {
            var title1 = MovieDbProvider.GetComparableName(str1);
            var title2 = MovieDbProvider.GetComparableName(str2);
            Assert.AreEqual(title1, title2);
        }

        [TestMethod]
        public void TestSpecialCharCleanup()
        {
            Compare("À bout de souffle", " A bout de souffle ");
            Compare ("  hello       world", "hello world");
            Compare("big day out, the", "big day out"); 
            Compare("Face Off" , "Face/Off");
        }

        [TestMethod]
        public void TestFetching()
        {
            int count = 0;
            int matched = 0;
            List<string> nonmatches = new List<string>();
            //string s = Environment.CurrentDirectory;
            using (StreamReader sr = File.OpenText(@"..\..\..\TestMediaBrowser\movies.txt"))
            {
                string line = sr.ReadLine();
                while(line!=null)
                {
                    string match;
                    string name = Helper.RemoveCommentsFromName(Path.GetFileName(line));
                    string[] possibles;
                    string id=MovieDbProvider.FindId(name, out match, out possibles);
                    count++;
                    if (match == null)
                    {
                        nonmatches.Add(name);
                        Debug.WriteLine(name + " not matched");
                        if (possibles != null)
                            Debug.WriteLine("\t" + string.Join("\n\t", possibles));
                        else
                            Debug.WriteLine("\tNo possible matches");
                    }
                    else
                    {
                        matched++;
                        Debug.WriteLine(name + " matched with " + match);
                        Debug.WriteLine(string.Format("http://api.themoviedb.org/2.0/Movie.getInfo?id={0}&api_key={1}", id,"f6bd687ffa63cd282b6ff2c6877f2669"));
                    }
                    /*
                        FileSystemSource source = new FileSystemSource(line, ItemType.Movie);
                    Item itm = source.ConstructItem();
                    itm.CreateEmptyMetadata();
                    
                    MediaMetadataStore store = new MediaMetadataStore(itm.UniqueName);
                    string[] possibles;
                    provider.Fetch(itm, ItemType.Movie, store, false, out possibles);
                    if (store.Name!=null)
                        Debug.WriteLine(source.Name + " matched " + store.Name);
                    else
                        Debug.WriteLine(source.Name + " not matched");
                     */
                    line = sr.ReadLine();
                }
                
            }
            Debug.WriteLine(string.Format("Fetching matched {0}/{1}", matched, count));
            Debug.WriteLine("The following were not matched: ");
            Debug.WriteLine(string.Join("\n", nonmatches.ToArray()));
        }

        [TestMethod]
        public void TestSpecificMovieMatch()
        {
            string name = "E.T. The Extra-Terrestrial";
            string match;
            string[] possibles;
            MovieDbProvider.FindId(name, out match, out possibles);
            if (match == null)
            {
                Debug.WriteLine(name + " not matched");
                if (possibles != null)
                    Debug.WriteLine("\t" + string.Join("\n\t", possibles));
                else
                    Debug.WriteLine("\tNo possible matches");
            }
            else
                Debug.WriteLine(name + " matched with " + match);
        }
    }
}
