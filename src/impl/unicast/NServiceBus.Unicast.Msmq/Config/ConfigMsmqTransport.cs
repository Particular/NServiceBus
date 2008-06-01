using ObjectBuilder;
using System.Configuration;

namespace NServiceBus.Unicast.Transport.Msmq.Config
{
    public class ConfigMsmqTransport
    {
        public ConfigMsmqTransport(IBuilder builder)
        {
            transport = builder.ConfigureComponent<MsmqTransport>(ComponentCallModelEnum.Singleton);

            MsmqTransportConfig cfg =
                ConfigurationManager.GetSection("MsmqTransportConfig") as MsmqTransportConfig;

            if (cfg == null)
                throw new ConfigurationErrorsException("Could not find configuration section for Msmq Transport.");

            transport.InputQueue = cfg.InputQueue;
            transport.NumberOfWorkerThreads = cfg.NumberOfWorkerThreads;
            transport.ErrorQueue = cfg.ErrorQueue;
            transport.MaxRetries = cfg.MaxRetries;
        }

        private readonly MsmqTransport transport;


        public ConfigMsmqTransport IsTransactional(bool value)
        {
            transport.IsTransactional = value;
            return this;
        }

        public ConfigMsmqTransport PurgeOnStartup(bool value)
        {
            transport.PurgeOnStartup = value;
            return this;
        }
    }
}
