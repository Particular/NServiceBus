using System;
using System.Configuration;
using Common.Logging;
using NServiceBus.Unicast.Transport.Msmq;
using NServiceBus.Unicast.Transport.Msmq.Config;
using ObjectBuilder;

namespace NServiceBus.Proxy.Host
{
    class Program
    {
        static void Main(string[] args)
        {

            try
            {
                LogManager.GetLogger("hello").Debug("Started.");
                IBuilder builder = new ObjectBuilder.SpringFramework.Builder();

                Proxy p = ConfigureSelfWith(builder);
                p.Start();

                Console.Read();
            }
            catch (Exception e)
            {
                LogManager.GetLogger("hello").Fatal("Exiting", e);
                Console.Read();
            }
        }

        static Proxy ConfigureSelfWith(IBuilder builder)
        {
            NServiceBusProxyConfig cfg = ConfigurationManager.GetSection("NServiceBusProxyConfig") as NServiceBusProxyConfig;

            if (cfg == null)
                throw new ConfigurationErrorsException("Could not find configuration section for UnicastBus.");

            //NServiceBus.Serializers.Configure.InterfaceToXMLSerializer.WithNameSpace("http://www.UdiDahan.com").With(builder);
            NServiceBus.Serializers.Configure.XmlSerializer.WithNameSpace("http://www.UdiDahan.com").With(builder);

            new ConfigMsmqTransport(builder)
                .IsTransactional(true)
                .PurgeOnStartup(false);

            builder.ConfigureComponent<InMemoryProxyDataStorage.Storage>(
                ComponentCallModelEnum.Singleton);

            builder.ConfigureComponent<Proxy>(ComponentCallModelEnum.Singleton)
                .RemoteServer = cfg.RemoteServer;

            Proxy p = builder.Build<Proxy>();
            builder.Build<MsmqTransport>().SkipDeserialization = true;

            return p;
        }
    }
}
