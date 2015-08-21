namespace NServiceBus.AcceptanceTests.Exceptions
{
    using System;
    using System.Diagnostics;
    using System.Globalization;
    using System.Linq;
    using System.Threading;
    using NUnit.Framework;

    static class StackTraceAssert
    {
        public static void StartsWith(string expected, Exception exception)
        {
            var translatedStackTrace = string.Empty;

            var thread = new Thread(() =>
            {
                // StackTrace strips away file names and line numbers
                translatedStackTrace = new StackTrace(exception, false).ToString();
            });
            thread.CurrentUICulture = new CultureInfo("en");
            thread.Start();
            thread.Join();

            var filteredStackTrace = string.Join(Environment.NewLine, translatedStackTrace
                .Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries)
                .Select(line => line.Trim())
                .Where(expected.Contains));

            Assert.AreEqual(expected, filteredStackTrace);
        }
    }
}

