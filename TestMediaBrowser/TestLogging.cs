using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MediaBrowser.Library.Logging;
using NUnit.Framework;

namespace TestMediaBrowser {
    [TestFixture]
    public class TestLogging {
        [Test]
        public void TestLogMessagePersistance() {
            LogRow message = new LogRow()
            {
                Category = "aa,aa",
                Message = "hello\nworld",
                Severity = LogSeverity.Error,
                ThreadId = 100,
                ThreadName = "my big fat thread",
                Time = DateTime.Now
            };

            var copy = LogRow.FromString(message.ToString());

            Assert.AreEqual(message.Category, copy.Category);
            Assert.AreEqual(message.Message, copy.Message);
            Assert.AreEqual(message.Severity, copy.Severity);
            Assert.AreEqual(message.ThreadId, copy.ThreadId);
            Assert.AreEqual(message.ThreadName, copy.ThreadName);
            Assert.AreEqual(message.Time.Millisecond, copy.Time.Millisecond);
            Assert.AreEqual(message.Time.Second, copy.Time.Second);
            Assert.AreEqual(message.Time.Day, copy.Time.Day);
        }

        [Test]
        public void TestWritingLog() {
            FileLogger logger = new FileLogger(Environment.CurrentDirectory);
            logger.ReportError("something bad happened");
            logger.ReportException("Bla happened",new Exception("hello"));
            logger.Flush();
        }
    }
}
