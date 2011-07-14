using System;
using System.Collections.Specialized;
using System.Linq;
using Common.Logging;
using NServiceBus.Utils;

namespace Runner
{
    class Program
    {
        static void Main(string[] args)
        {
            var props = new NameValueCollection();
            props["configType"] = "EXTERNAL";
            LogManager.Adapter = new Common.Logging.Log4Net.Log4NetLoggerFactoryAdapter(props);

            var layout = new log4net.Layout.PatternLayout("%d [%t] %-5p %c [%x] <%X{auth}> - %m%n");
            var level = log4net.Core.Level.Debug;

            var appender = new log4net.Appender.ConsoleAppender
            {
                Layout = layout,
                Threshold = level
            };
            log4net.Config.BasicConfigurator.Configure(appender);

            if(!MsmqInstallation.IsInstallationGood())
            {
                Console.WriteLine("MSMQ is not configured correctly for use with NServiceBus");

                if(!args.ToList().Contains("/i"))
                {
                    Console.WriteLine("Please run with /i to reconfigure MSMQ");
                    return;
                }
            }
            MsmqInstallation.StartMsmqIfNecessary();

            DtcUtil.StartDtcIfNecessary();

            PerformanceCounterInstallation.InstallCounters();
        }
    }
}
