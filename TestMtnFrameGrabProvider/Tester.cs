using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using MtnFrameGrabProvider;

namespace TestMtnFrameGrabProvider {

    [TestFixture]
    public class Tester {

        [Test]
        public void TestFileExtracts() {
            Plugin.EnsureMtnIsExtracted();
        }

        [Test]
        public void TestTumbnailing() {
            ThumbCreator.CreateThumb(@"C:\Users\sam\Desktop\videos 123\01.avi", @"C:\Users\sam\Desktop\videos 123\hello2.jpg", 600); 
        } 
    }

}
