using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using MediaBrowser.Library.Extensions;
using System.Collections;

namespace TestMediaBrowser {
    [TestFixture]
    public class TestExtensions {
        
        [Test]
        public void TestDistinctExtension() {
            var foos = Enumerable.Range(0, 10).Select(i => new { Id = Guid.NewGuid(), name = i.ToString() }).ToList();
            var duplicates = foos.Concat(foos);

            var clean = duplicates.Distinct(item => item.Id).ToList();

            Assert.AreEqual(foos.Count(), clean.Count());
        }
    }
}
