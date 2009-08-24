using System.Collections.Specialized;
using Common.Logging;
using NServiceBus.Host.Profiles;
using NServiceBus.ObjectBuilder;

namespace NServiceBus.Host.Internal.ProfileHandlers
{
    /// <summary>
    /// Configures the infrastructure for the Lite profile
    /// </summary>
    public class LiteProfileHandler : IHandleProfileConfiguration<Lite>
    {
        void IHandleProfile.Init(IConfigureThisEndpoint specifier) {}

        void IHandleProfileConfiguration.ConfigureLogging()
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
        }

        void IHandleProfileConfiguration.ConfigureSagas(Configure busConfiguration)
        {
            Configure.TypeConfigurer.ConfigureComponent<InMemorySagaPersister>(ComponentCallModelEnum.Singleton);
        }

        void IHandleProfileConfiguration.ConfigureSubscriptionStorage(Configure busConfiguration)
        {
            Configure.TypeConfigurer.ConfigureComponent<InMemorySubscriptionStorage>(
                ComponentCallModelEnum.Singleton);
        }
    }
}
