using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using MediaBrowser.Library.Extensions;
using System.ComponentModel;
using MediaBrowser.LibraryManagement;

namespace TestMediaBrowser {
    [TestFixture]
    public class TestInterceptor {

        class Foo : MarshalByRefObject, IInterceptorNotifiable {
            public int PublicProp { get; set; }
            public string Str { get; set; }
            public string lastPropertyChanged;

            public void OnPropertyChanged(string propertyName) {
                lastPropertyChanged = propertyName;
            }

        }


        [Test]
        public void TestPropertyInterception() {

            var foo = Interceptor<Foo>.Create();
            foo.PublicProp = 100;
            
            Assert.AreEqual("PublicProp", foo.lastPropertyChanged);

        }
    }
}
