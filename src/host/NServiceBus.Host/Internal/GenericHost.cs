using System;
using System.Diagnostics;
using Common.Logging;
using System.Collections.Specialized;

namespace NServiceBus.Host.Internal
{
    public class GenericHost : MarshalByRefObject
    {
        public void Start()
        {
            Trace.WriteLine("Starting host for " + endpointType.FullName);

            var configurationSpecifier = (IConfigureThisEndpoint)Activator.CreateInstance(endpointType);
          
            ConfigureLogging(configurationSpecifier);


            var busConfiguration = new ConfigurationBuilder(configurationSpecifier, endpointType).Build();
          
            Action startupAction = null;

            if (configurationSpecifier is ISpecify.StartupAction)
                startupAction = (configurationSpecifier as ISpecify.StartupAction).StartupAction;

            messageEndpoint = Configure.ObjectBuilder.Build<IMessageEndpoint>();

            if (!(configurationSpecifier is IDontWant.TheBusStartedAutomatically))
                busConfiguration.CreateBus().Start(startupAction);

            if (messageEndpoint != null)
                messageEndpoint.OnStart();
        }

       
        private static void ConfigureLogging(IConfigureThisEndpoint specifier)
        {
            if (specifier is IDontWant.Log4Net)
                LogManager.Adapter = (specifier as IDontWant.Log4Net).UseThisInstead;
            else
            {
                var props = new NameValueCollection();
                props["configType"] = "INLINE";
                LogManager.Adapter = new Common.Logging.Log4Net.Log4NetLoggerFactoryAdapter(props);

                if (specifier is ISpecify.MyOwnLog4NetConfiguration)
                    (specifier as ISpecify.MyOwnLog4NetConfiguration).ConfigureLog4Net();
                else
                {
                    var layout = new log4net.Layout.PatternLayout("%d [%t] %-5p %c [%x] <%X{auth}> - %m%n");
                    var level = (specifier is ISpecify.LoggingLevel
                                     ? (specifier as ISpecify.LoggingLevel).Level
                                     : log4net.Core.Level.Debug);

                    var appender = new log4net.Appender.ConsoleAppender
                                       {
                                           Layout = layout,
                                           Threshold = level
                                       };
                    log4net.Config.BasicConfigurator.Configure(appender);
                }
            }
        }

        public void Stop()
        {
            if (messageEndpoint != null)
                messageEndpoint.OnStop();
        }

        public GenericHost(Type endpointType)
        {
            this.endpointType = endpointType;
        }

        private readonly Type endpointType;
        private IMessageEndpoint messageEndpoint;
    }
}