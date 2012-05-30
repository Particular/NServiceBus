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
            if (SetLoggingLibrary.Log4NetExists)
                SetLoggingLibrary.Log4Net(null, Logging.Loggers.Log4NetAdapter.AppenderFactory.CreateRollingFileAppender(null, "logfile"));
            else if (SetLoggingLibrary.NLogExists)
                SetLoggingLibrary.NLog(Logging.Loggers.NLogAdapter.TargetFactory.CreateRollingFileTarget("logfile"));

            if (GetStdHandle(STD_OUTPUT_HANDLE) == IntPtr.Zero)
                return;

            if (SetLoggingLibrary.Log4NetExists)
                SetLoggingLibrary.Log4Net(null, Logging.Loggers.Log4NetAdapter.AppenderFactory.CreateColoredConsoleAppender("Info"));
            else if (SetLoggingLibrary.NLogExists)
                SetLoggingLibrary.NLog(Logging.Loggers.NLogAdapter.TargetFactory.CreateColoredConsoleTarget());
        }

        [DllImport("kernel32.dll", SetLastError = true)]
        static extern IntPtr GetStdHandle(int nStdHandle);
        const int STD_OUTPUT_HANDLE = -11;
    }
}