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
            bool logToConsole = GetStdHandle(STD_OUTPUT_HANDLE) != IntPtr.Zero;

            if (Logging.Loggers.Log4NetAdapter.Log4NetConfigurator.Log4NetExists)
            {
                SetLoggingLibrary.Log4Net(null, Logging.Loggers.Log4NetAdapter.Log4NetAppenderFactory.CreateRollingFileAppender(null, "logfile"));

                if (logToConsole)
                    SetLoggingLibrary.Log4Net(null, Logging.Loggers.Log4NetAdapter.Log4NetAppenderFactory.CreateColoredConsoleAppender("Info"));
            }
            //else if (Logging.Loggers.NLogAdapter.NLogConfigurator.NLogExists)
            //{
            //    const string layout = "${longdate}|${level:uppercase=true}|${logger}|${message}${onexception:${newline}${exception:format=tostring}}";

            //    var targets = new List<object> { Logging.Loggers.NLogAdapter.TargetFactory.CreateRollingFileTarget("logfile", layout) };

            //    if (logToConsole)
            //        targets.Add(Logging.Loggers.NLogAdapter.TargetFactory.CreateColoredConsoleTarget(layout));

            //    SetLoggingLibrary.NLog(null, targets.ToArray());
            //}
            else
                Internal.ConfigureInternalLog4Net.Production(logToConsole);
        }

        [DllImport("kernel32.dll", SetLastError = true)]
        static extern IntPtr GetStdHandle(int nStdHandle);
        const int STD_OUTPUT_HANDLE = -11;
    }
}