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


        [Test]
        public void TestItemsCanBeSetCorrectly() {
            Directory.CreateDirectory(path);

            var store = new FileBasedDictionary<Dog>(path);

            Guid id = Guid.NewGuid();
            var dog = new Dog() { Age = 100 };
            store[id] = dog;

            Assert.AreEqual(dog, store[id]);
            store.Dispose();

            Directory.Delete(path, true);
        }

        [Test]
        public void TestItemsCanBeLoadedFromStore() {

            Directory.CreateDirectory(path);

            var store = new FileBasedDictionary<Dog>(path);

            Guid id = Guid.NewGuid();
            var dog = new Dog() { Age = 100 };
            store[id] = dog;

            var store2 = new FileBasedDictionary<Dog>(path);
            Assert.AreEqual(100, store2[id].Age);

            store2.Dispose();
            store.Dispose();

            Directory.Delete(path, true);
        }

        [Test]
        public void TestItemsForceOtherStoresToRefresh() {

            Directory.CreateDirectory(path);
            
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

            Directory.Delete(path, true);
        }

        [Test]
        public void TestFastLoadWorksCorrectly() {

            Directory.CreateDirectory(path);

            var store = new FileBasedDictionary<Dog>(path,false);

            Guid id = Guid.NewGuid();
            Dog dog = new Dog();
            dog.Age = 20;
            store[id] = dog; 

            // as a side effect we will have the fastload file
            store.Validate();

            dog.Age = 21;
            store[id] = dog;


            var store2 = new FileBasedDictionary<Dog>(path, false);
            store2.LoadFastLoadData();
            store2.Validate();

            Assert.AreEqual(21, store2[id].Age);

            store.Dispose();
            store2.Dispose();

            Directory.Delete(path, true);
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
