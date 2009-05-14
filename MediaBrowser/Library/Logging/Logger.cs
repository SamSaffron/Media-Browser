using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MediaBrowser.Library.Factories;
using MediaBrowser.Library.Configuration;

namespace MediaBrowser.Library.Logging {

    /// <summary>
    /// This is the class you should use for logging, it redirects all the calls to the Kernel.
    /// </summary>
    public static class Logger {

        static Logger() {
            // our default logger
            LoggerInstance = new TraceLogger();
        }

        public static ILogger LoggerInstance { get; set; }

        public static void ReportVerbose(string message) {
            LoggerInstance.ReportVerbose(message);
        }

        public static void ReportVerbose(string message, string category) {
            LoggerInstance.ReportVerbose(message, category);
        }

        public static void ReportInfo(string message) {
            LoggerInstance.ReportInfo(message);
        }

        public static void ReportInfo(string message, string category) {
            LoggerInstance.ReportInfo(message, category);
        }

        public static void ReportWarning(string message) {
            LoggerInstance.ReportWarning(message);
        }

        public static void ReportWarning(string message, string category) {
            LoggerInstance.ReportWarning(message, category);
        }

        public static void ReportException(string message, Exception exception) {
            LoggerInstance.ReportException(message, exception);
        }

        public static void ReportException(string message, Exception exception, string category) {
            LoggerInstance.ReportException(message, exception, category);
        }

        public static void ReportError(string message) {
            LoggerInstance.ReportError(message);
        }

        public static void ReportError(string message, string category) {
            LoggerInstance.ReportError(message, category);
        }
    }
}
