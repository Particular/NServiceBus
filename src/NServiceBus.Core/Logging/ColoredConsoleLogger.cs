namespace NServiceBus
{
    using System;
    using System.IO;
    using Logging;

    static class ColoredConsoleLogger
    {
        static ColoredConsoleLogger()
        {
            using (var stream = Console.OpenStandardOutput())
            {
                logToConsole = stream != Stream.Null;
            }
        }

        public static void Write(string message, LogLevel logLevel)
        {
            if (!logToConsole)
            {
                return;
            }

            try
            {
                Console.ForegroundColor = GetColor(logLevel);
                Console.WriteLine(message);
            }
            finally
            {
                Console.ResetColor();
            }
        }

        static ConsoleColor GetColor(LogLevel logLevel)
        {
            if (logLevel >= LogLevel.Error)
            {
                return ConsoleColor.Red;
            }

            if (logLevel == LogLevel.Warn)
            {
                return ConsoleColor.DarkYellow;
            }

            return ConsoleColor.White;
        }

        static bool logToConsole;
    }
}