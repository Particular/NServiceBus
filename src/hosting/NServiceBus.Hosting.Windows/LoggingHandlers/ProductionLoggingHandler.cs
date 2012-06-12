namespace NServiceBus.Hosting.Windows.LoggingHandlers
{
    using System;
    using System.Runtime.InteropServices;
    using System.Collections.Generic;

    /// <summary>
    /// Handles logging configuration for the production profile
    /// </summary>
    public class ProductionLoggingHandler : IConfigureLoggingForProfile<Production>
    {
        void IConfigureLogging.Configure(IConfigureThisEndpoint specifier)
        {
            if (SetLoggingLibrary.Log4NetExists)
            {
                SetLoggingLibrary.Log4Net(null, Logging.Loggers.Log4NetAdapter.AppenderFactory.CreateRollingFileAppender(null, "logfile"));

                if (GetStdHandle(STD_OUTPUT_HANDLE) == IntPtr.Zero)
                    return;

                SetLoggingLibrary.Log4Net(null, Logging.Loggers.Log4NetAdapter.AppenderFactory.CreateColoredConsoleAppender("Info"));
            }
            else if (SetLoggingLibrary.NLogExists)
            {
                const string layout = "${longdate}|${level:uppercase=true}|${logger}|${message}${onexception:${newline}${exception:format=tostring}}";

                var targets = new List<object> { Logging.Loggers.NLogAdapter.TargetFactory.CreateRollingFileTarget("logfile", layout) };

                if (GetStdHandle(STD_OUTPUT_HANDLE) != IntPtr.Zero)
                    targets.Add(Logging.Loggers.NLogAdapter.TargetFactory.CreateColoredConsoleTarget(layout));

                SetLoggingLibrary.NLog(null, targets.ToArray());
            }
            else
                Internal.ConfigureInternalLog4Net.Production();
            //                throw new ConfigurationErrorsException("No logging framework found. NServiceBus supports log4net and NLog. You need to put any of these in the same directory as the host.");
        }

        [DllImport("kernel32.dll", SetLastError = true)]
        static extern IntPtr GetStdHandle(int nStdHandle);
        const int STD_OUTPUT_HANDLE = -11;
    }
}