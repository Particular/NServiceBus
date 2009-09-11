using System.Collections.Specialized;
using Common.Logging;
using NServiceBus.Host.Profiles;
using NServiceBus.Utils.Reflection;
using NServiceBus.Unicast.Subscriptions;
using NServiceBus.ObjectBuilder;

namespace NServiceBus.Host.Internal.ProfileHandlers
{
    /// <summary>
    /// Configures the infrastructure for the Production profile.
    /// </summary>
    public class ProductionProfileHandler : IHandleProfileConfiguration<Production>
    {
        private IConfigureThisEndpoint spec;

        void IHandleProfileConfiguration.Init(IConfigureThisEndpoint specifier)
        {
            spec = specifier;
        }

        void IHandleProfileConfiguration.ConfigureLogging()
        {
            if (spec is IDontWantLog4Net)
                LogManager.Adapter = (spec as IDontWantLog4Net).UseThisInstead;
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
                busConfiguration.NHibernateSagaPersister();
        }

        void IHandleProfileConfiguration.ConfigureSubscriptionStorage(Configure busConfiguration)
        {
            if (spec is ISpecify.MyOwn.SubscriptionStorage)
                return;

            var storageType = spec.GetType().GetGenericallyContainedType(typeof (ISpecify.ToUse.SubscriptionStorage<>),
                                                                         typeof (ISubscriptionStorage));

            if (storageType != null)
                Configure.TypeConfigurer.ConfigureComponent(storageType, ComponentCallModelEnum.Singleton);
            else
                busConfiguration.DBSubcriptionStorage();
        }
    }
}
