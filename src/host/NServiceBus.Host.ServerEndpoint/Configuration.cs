using System.IO;
using System.Reflection;
using log4net.Config;

namespace NServiceBus.Host.ServerEndpoint
{
    public class Configuration : IMessageEndpointConfiguration
    {
        public Configuration()
        {
            //setup log4net
            XmlConfigurator.ConfigureAndWatch(new FileInfo(Assembly.GetExecutingAssembly().ManifestModule.Name + ".config"));

        }

        public Configure ConfigureBus(Configure config)
        {
            return config
                .SpringBuilder()
                .MsmqSubscriptionStorage()
                .XmlSerializer()
                .MsmqTransport()
                    .IsTransactional(true)
                    .PurgeOnStartup(false)
                .UnicastBus()
                    .ImpersonateSender(false);

        }
      
    }
}