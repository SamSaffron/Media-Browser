using System;
using System.Text;
using System.Collections.Generic;

using NUnit.Framework;
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
    [TestFixture]
    public class TestTheMovieDB
    {
        public TestTheMovieDB()
        {
        }
        
        [SetUp]
        public void Setup()
        {
            typeof(Microsoft.MediaCenter.UI.Application).GetMethod("RegisterUIThread", BindingFlags.NonPublic | BindingFlags.Static).Invoke(null, null);
        }


        private void Compare(string str1, string str2)
        {
            var title1 = MovieDbProvider.GetComparableName(str1);
            var title2 = MovieDbProvider.GetComparableName(str2);
            Assert.AreEqual(title1, title2);
        }

        [Test]
        public void TestSpecialCharCleanup()
        {
            Compare("À bout de souffle", " A bout de souffle ");
            Compare ("  hello       world", "hello world");
            Compare("big day out, the", "big day out"); 
            Compare("Face Off" , "Face/Off");
        }

        [Test]
        public void TestCorrectMovieTitleIsFetched() 
        { 
            string matchedName; 
            string[] possibles;
            MovieDbProvider.AttemptFindId("City Of Men", "", out matchedName, out possibles);

            Assert.AreEqual("City Of Men".ToLower(), matchedName.ToLower()); 
        }

        [Test]
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

        [Test]
        public void TestSpecificMovieMatch()
        {
            string name = "Flight of the Phoenix (2004)";
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
