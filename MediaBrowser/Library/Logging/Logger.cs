using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.Threading;

namespace MediaBrowser.Library.Logging
{

    public abstract class Logger : IDisposable, ILogger
    {


        public Logger()
        {
            Enabled = true;
        }

        public bool Enabled { get; set; }

        public LogSeverity Severity { get; set; }

        public void ReportVerbose(string message)
        {
            ReportVerbose(message, "");
        }

        public void ReportVerbose(string message, string category)
        {
            LogMessage(LogSeverity.Verbose, message, category);
        }

        public void ReportInfo(string message)
        {
            ReportInfo(message, "");
        }

        public void ReportInfo(string message, string category)
        {
            LogMessage(LogSeverity.Info, message, category);
        }

        public void ReportWarning(string message)
        {
            LogMessage(LogSeverity.Warning, message, "");
        }

        public void ReportWarning(string message, string category)
        {
            LogMessage(LogSeverity.Warning, message, category);
        }

        public void ReportException(string message, Exception exception)
        {
            ReportException(message, exception, "");
        }

        public void ReportException(string message, Exception exception, string category)
        {

            StringBuilder builder = new StringBuilder();
            if (exception != null)
            {
                builder.AppendFormat("Unhandled exception.  Type={0} Msg={1} Src={2}{4}StackTrace={4}{3}",
                    exception.GetType().FullName,
                    exception.Message,
                    exception.Source,
                    exception.StackTrace,
                    Environment.NewLine);
            }
            StackFrame frame = new StackFrame(1);
            ReportError(string.Format("{0} ( {1} )", message, builder), string.Format("{0}.{1}", frame.GetMethod().ReflectedType.FullName, frame.GetMethod().Name));
        }

        public void ReportError(string message)
        {
            ReportError(message, "");
        }

        public void ReportError(string message, string category)
        {
            LogMessage(LogSeverity.Error, message, category);
        }


        void LogMessage(LogSeverity severity, string message)
        {
            LogMessage(severity, message, "");
        }

        void LogMessage(LogSeverity severity, string message, string category)
        {

            if (!Enabled) return;

            string threadName = Thread.CurrentThread.Name;
            int threadId = Thread.CurrentThread.ManagedThreadId;
            DateTime now = DateTime.Now;

            LogRow row = new LogRow()
            {
                Severity = severity,
                Message = message,
                Category = category,
                ThreadId = threadId,
                ThreadName = threadName,
                Time = now
            };

            LogMessage(row);
        }

        public virtual void Flush()
        {
        }

        public abstract void LogMessage(LogRow row);



        #region IDisposable Members

        void IDisposable.Dispose()
        {
            // todo signal a queue flush and terminate background thread using a signal 

            // since its a BG thread its not critical to implement this  
        }

        #endregion
    }
}
