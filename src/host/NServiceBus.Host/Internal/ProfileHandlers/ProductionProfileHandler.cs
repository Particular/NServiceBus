using System.Collections.Specialized;
using Common.Logging;
using NServiceBus.Host.Profiles;
namespace NServiceBus.Host.Internal.ProfileHandlers
{
    public class ProductionProfileHandler : IHandleProfile<Production>
    {
        private IConfigureThisEndpoint specifier;

        void IHandleProfile.Init(IConfigureThisEndpoint specifier)
        {
            this.specifier = specifier;
        }

        void IHandleProfile.ConfigureLogging()
        {
            if (specifier is IDontWant.Log4Net)
                LogManager.Adapter = (specifier as IDontWant.Log4Net).UseThisInstead;
            else
            {
                var props = new NameValueCollection();
                props["configType"] = "EXTERNAL";
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

        void IHandleProfile.ConfigureSagas(Configure busConfiguration)
        {
            if (!(specifier is ISpecify.MyOwnSagaPersistence))
                busConfiguration.NHibernateSagaPersister();
        }

        void IHandleProfile.ConfigureSubscriptionStorage(Configure busConfiguration)
        {
            busConfiguration.DBSubcriptionStorage();
        }
    }
}
