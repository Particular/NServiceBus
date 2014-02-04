namespace NServiceBus.Hosting.Windows.LoggingHandlers
{
    using System;
    using System.Collections.Generic;
    using System.Runtime.InteropServices;
    using Internal;
    using Logging;
    using Logging.Loggers.Log4NetAdapter;
    using Logging.Loggers.NLogAdapter;

    /// <summary>
    /// Handles logging configuration for the production profile
    /// </summary>
    public class ProductionLoggingHandler : IConfigureLoggingForProfile<Production>
    {
        void IConfigureLogging.Configure(IConfigureThisEndpoint specifier)
        {
            if (LogManager.IsConfigured)
                return;

            var logToConsole = GetStdHandle(STD_OUTPUT_HANDLE) != IntPtr.Zero;

            if (Log4NetConfigurator.Log4NetExists)
            {
                SetLoggingLibrary.Log4Net(null, Log4NetAppenderFactory.CreateRollingFileAppender(null, "logfile"));

                if (logToConsole)
                    SetLoggingLibrary.Log4Net(null, Log4NetAppenderFactory.CreateColoredConsoleAppender("Info"));
            }
            else if (NLogConfigurator.NLogExists)
            {
                const string layout = "${longdate}|${level:uppercase=true}|${logger}|${message}${onexception:${newline}${exception:format=tostring}}";

                var targets = new List<object> {NLogTargetFactory.CreateRollingFileTarget("logfile", layout)};

                if (logToConsole)
                {
                    targets.Add(NLogTargetFactory.CreateColoredConsoleTarget(layout));
                }

                SetLoggingLibrary.NLog(null, targets.ToArray());
            }
            else
            {
                ConfigureInternalLog4Net.Production(logToConsole);
            }
        }

        [DllImport("kernel32.dll", SetLastError = true)]
        static extern IntPtr GetStdHandle(int nStdHandle);
        const int STD_OUTPUT_HANDLE = -11;
    }
}