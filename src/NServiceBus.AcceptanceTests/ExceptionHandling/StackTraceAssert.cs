using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using NUnit.Framework;

static class StackTraceAssert
{
    public static void AreEqual(string expected, string actual)
    {
        if (actual == null || expected == null)
        {
            Assert.AreEqual(expected, actual);
        }
        else
        {
            var cleanStackTrace = actual.CleanStackTrace();
            try
            {
                Assert.AreEqual(expected.Replace("\r\n", "\n"), cleanStackTrace.Replace("\r\n", "\n"));
            }
            catch (Exception)
            {
                Trace.WriteLine(cleanStackTrace);
                throw;
            }
        }
    }
    public static string CleanStackTrace(this string stackTrace)
    {
        if (stackTrace == null)
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

