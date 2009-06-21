using System;
using System.Configuration;
using Common.Logging;
using NServiceBus.Proxy.InMemoryImpl;
using NServiceBus;
using NServiceBus.ObjectBuilder;
using NServiceBus.Unicast;
using NServiceBus.Unicast.Transport.Msmq;
using NServiceBus.Serialization;

namespace NServiceBus.Proxy.Host
{
    class Program
    {
        static void Main(string[] args)
        {

            try
            {
                LogManager.GetLogger("hello").Debug("Started.");

                var config = ConfigurationManager.GetSection("NServiceBusProxyConfig") as NServiceBusProxyConfig;

                if (config == null)
                    throw new ConfigurationErrorsException("Could not find configuration section for UnicastBus.");

                MsmqTransport externalTransport = null;
                MsmqTransport internalTransport = null;

                NServiceBus.Configure.With()
                    .SpringBuilder(
                    (cfg) =>
                        {
                            var numberOfThreads = int.Parse(ConfigurationManager.AppSettings["NumberOfWorkerThreads"]);
                            var maxRetries = int.Parse(ConfigurationManager.AppSettings["MaxRetries"]);
                            var errorQueue = ConfigurationManager.AppSettings["ErrorQueue"];

                            externalTransport = new MsmqTransport
                                                    {
                                                        InputQueue = ConfigurationManager.AppSettings["ExternalQueue"],
                                                        NumberOfWorkerThreads = numberOfThreads,
                                                        MaxRetries = maxRetries,
                                                        ErrorQueue = errorQueue,
                                                        IsTransactional = true,
                                                        PurgeOnStartup = false,
                                                        SkipDeserialization = true
                                                    };

                            internalTransport = new MsmqTransport
                                                    {
                                                        InputQueue = ConfigurationManager.AppSettings["InternalQueue"],
                                                        NumberOfWorkerThreads = numberOfThreads,
                                                        MaxRetries = maxRetries,
                                                        ErrorQueue = errorQueue,
                                                        IsTransactional = true,
                                                        PurgeOnStartup = false,
                                                    };


                            cfg.ConfigureComponent<ProxyDataStorage>(ComponentCallModelEnum.Singleton);
                            cfg.ConfigureComponent<SubscriberStorage>(ComponentCallModelEnum.Singleton);

                            cfg.ConfigureComponent<Proxy>(ComponentCallModelEnum.Singleton)
                                .ConfigureProperty((x) => x.RemoteServer, config.RemoteServer);
                        }
                    )
                    .XmlSerializer("http://UdiDahan.com")
                    .UnicastBus()
                    .CreateBus()
                    .Start(
                    builder =>
                        {
                            var serializer = builder.Build<IMessageSerializer>();
                            internalTransport.MessageSerializer = serializer;
                            internalTransport.Builder = builder;

                            var bus = builder.Build<UnicastBus>();
                            bus.Transport = internalTransport;

                            externalTransport.MessageSerializer = serializer;

                            var proxy = builder.Build<Proxy>();
                            proxy.ExternalTransport = externalTransport;
                            proxy.InternalTransport = internalTransport;

                            proxy.Start();
                        }
                    );

                Console.Read();
            }
            catch (Exception e)
            {
                LogManager.GetLogger("hello").Fatal("Exiting", e);
                Console.Read();
            }
        }
    }
}
