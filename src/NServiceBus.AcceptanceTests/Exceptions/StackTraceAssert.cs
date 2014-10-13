using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using NUnit.Framework;

namespace NServiceBus.AcceptanceTests.Exceptions
{
    static class StackTraceAssert
    {
        public static void StartsWith(string expected, string actual)
        {
            if (actual == null)
            {
                Assert.Fail();
            }
            else
            {
                var cleanStackTrace = CleanStackTrace(actual);

                var reader = new StringReader(cleanStackTrace);

                var stringBuilder = new StringBuilder();
                while (true)
                {
                    var actualLine = reader.ReadLine();
                    if (actualLine == null)
                    {
                        break;
                    }
                    if (expected.Contains(actualLine))
                    {
                        stringBuilder.AppendLine(actualLine);
                    }
                }

                try
                {
                    actual = stringBuilder.ToString().TrimEnd();
                    Assert.AreEqual(actual, expected);
                }
                catch (Exception)
                {
                    Trace.WriteLine(cleanStackTrace);
                    throw;
                }
            }
        }
        static string CleanStackTrace(string stackTrace)
        {
            if (stackTrace== null)
            {
                return string.Empty;
            }
            using (var stringReader = new StringReader(stackTrace))
            {
                var stringBuilder = new StringBuilder();
                while (true)
                {
                    var line = stringReader.ReadLine();
                    if (line == null)
                    {
                        break;
                    }

                    stringBuilder.AppendLine(line.Split(new[]
                    {
                        " in "
                    }, StringSplitOptions.RemoveEmptyEntries).First().Trim());
                }
                return stringBuilder.ToString().Trim();
            }
        }
    }
}

