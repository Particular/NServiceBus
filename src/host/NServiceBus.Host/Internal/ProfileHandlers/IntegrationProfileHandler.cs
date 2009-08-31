using System.Collections.Specialized;
using Common.Logging;
using NServiceBus.Config;
using NServiceBus.Host.Profiles;
using NServiceBus.ObjectBuilder;
using NServiceBus.Unicast.Subscriptions.Msmq;

namespace NServiceBus.Host.Internal.ProfileHandlers
{
    /// <summary>
    /// Configures the infrastructure for the Integration profile.
    /// </summary>
    public class IntegrationProfileHandler : IHandleProfileConfiguration<Integration>
    {
        private IConfigureThisEndpoint spec;

        void IHandleProfile.Init(IConfigureThisEndpoint specifier)
        {
            spec = specifier;
        }

        void IHandleProfileConfiguration.ConfigureLogging()
        {
            if (spec is IDontWant.Log4Net)
                LogManager.Adapter = (spec as IDontWant.Log4Net).UseThisInstead;
            else
            {
                var props = new NameValueCollection();
                props["configType"] = "EXTERNAL";
                LogManager.Adapter = new Common.Logging.Log4Net.Log4NetLoggerFactoryAdapter(props);

                if (spec is ISpecify.MyOwn.Log4NetConfiguration)
                    (spec as ISpecify.MyOwn.Log4NetConfiguration).ConfigureLog4Net();
                else
                {
                    var layout = new log4net.Layout.PatternLayout("%d [%t] %-5p %c [%x] <%X{auth}> - %m%n");
                    var level = (spec is ISpecify.LoggingLevel
                                     ? (spec as ISpecify.LoggingLevel).Level
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

        void IHandleProfileConfiguration.ConfigureSagas(Configure busConfiguration)
        {
            if (!(spec is ISpecify.MyOwn.SagaPersistence))
                busConfiguration.NHibernateSagaPersisterWithSQLiteAndAutomaticSchemaGeneration();
        }

        void IHandleProfileConfiguration.ConfigureSubscriptionStorage(Configure busConfiguration)
        {

            if (Configure.GetConfigSection<MsmqSubscriptionStorageConfig>() == null)
            {
                string q = Program.GetEndpointId(spec.GetType()) + "_subscriptions";
                busConfiguration.Configurer.ConfigureComponent<MsmqSubscriptionStorage>(ComponentCallModelEnum.Singleton)
                    .ConfigureProperty(s => s.Queue, q);
            }
            else
            {
                busConfiguration.MsmqSubscriptionStorage();
            }
        }
    }
}
