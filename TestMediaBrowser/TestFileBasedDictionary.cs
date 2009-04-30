using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using NUnit.Framework;
using MediaBrowser.Library.Persistance;
using System.Threading;

namespace TestMediaBrowser {

    [TestFixture]
    public class TestFileBasedDictionary {

        string path = Path.Combine(Path.GetTempPath(), "temptest");

        [SetUp]
        public void Setup() {
            Directory.CreateDirectory(path);
        }

        [TearDown]
        public void Teardown() {
            Directory.Delete(path, true);
        }

        [Test]
        public void TestItemsCanBeSetCorrectly() {
            var store = new FileBasedDictionary<Dog>(path);

            Guid id = Guid.NewGuid();
            var dog = new Dog() { Age = 100 };
            store[id] = dog;

            Assert.AreEqual(dog, store[id]);
            store.Dispose();
        }

        [Test]
        public void TestItemsCanBeLoadedFromStore() {
            var store = new FileBasedDictionary<Dog>(path);

            Guid id = Guid.NewGuid();
            var dog = new Dog() { Age = 100 };
            store[id] = dog;

            var store2 = new FileBasedDictionary<Dog>(path);
            Assert.AreEqual(100, store2[id].Age);

            store2.Dispose();
            store.Dispose();
        }

        [Test]
        public void TestItemsForceOtherStoresToRefresh() { 
            
            var store = new FileBasedDictionary<Dog>(path);
            var store2 = new FileBasedDictionary<Dog>(path);
#if (DEBUG)
            store.TrackingId = "first";
            store2.TrackingId = "second";
#endif

            Guid id = Guid.NewGuid();
            var dog = new Dog() { Age = 100, OptionalName = "milo" };
            store[id] = dog;

            while (store2[id] == null) {
                Thread.Sleep(1);
            }

            Assert.AreEqual(100, store2[id].Age);

            var dog2 = store2[id]; 
            dog2.Age = 99;
            dog2.OptionalName = "milo2";
            store2[id] = dog2;

            while (dog.Age != 99) {
                Thread.Sleep(1);
            }

            Assert.IsTrue(dog.ChangeCount > 0);

            store.Dispose();
            store2.Dispose();
                   
        }


        class Dog : IPersistableChangeNotifiable {

            public int ChangeCount { get; set; }

            public string OptionalName { get; set; }

            [Persist]
            public int Age { get; set; }

            public void OnChanged() {
                lock (this) {
                    ChangeCount++;
                }
            }
        }
    }


    
}
