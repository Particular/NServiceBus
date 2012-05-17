using NServiceBus.Logging.Log4Net;

namespace NServiceBus.Hosting.Windows.LoggingHandlers
{
    using System;
    using System.Runtime.InteropServices;

    /// <summary>
    /// Handles logging configuration for the production profile
    /// </summary>
    public class ProductionLoggingHandler : IConfigureLoggingForProfile<Production>
    {
        void IConfigureLogging.Configure(IConfigureThisEndpoint specifier)
        {
            SetLoggingLibrary.Log4Net(null, AppenderFactory.CreateRollingFileAppender(null, "logfile"));

            if (GetStdHandle(STD_OUTPUT_HANDLE) == IntPtr.Zero)
                return;

            SetLoggingLibrary.Log4Net(null, AppenderFactory.CreateColoredConsoleAppender("Info"));
        }

        [DllImport("kernel32.dll", SetLastError = true)]
        static extern IntPtr GetStdHandle(int nStdHandle);
        const int STD_OUTPUT_HANDLE = -11;
    }
}