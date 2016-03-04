namespace NServiceBus
{
    using System;
    using System.Runtime.InteropServices;
    using Logging;

    static class ColoredConsoleLogger
    {
        static ColoredConsoleLogger()
        {
            const int STD_OUTPUT_HANDLE = -11;
            logToConsole = GetStdHandle(STD_OUTPUT_HANDLE) != IntPtr.Zero;
        }

        [DllImport("kernel32.dll", SetLastError = true)]
        static extern IntPtr GetStdHandle(int nStdHandle);

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