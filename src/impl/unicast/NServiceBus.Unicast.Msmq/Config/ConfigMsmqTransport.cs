using ObjectBuilder;
using System.Configuration;

namespace NServiceBus.Unicast.Transport.Msmq.Config
{
    public class ConfigMsmqTransport
    {
        public ConfigMsmqTransport(IBuilder builder)
        {
            this.config = builder.ConfigureComponent(typeof(MsmqTransport), ComponentCallModelEnum.Singleton);

            MsmqTransportConfig cfg =
                ConfigurationManager.GetSection("MsmqTransportConfig") as MsmqTransportConfig;

            if (cfg == null)
                throw new ConfigurationErrorsException("Could not find configuration section for Msmq Transport.");

            config
                .ConfigureProperty("InputQueue", cfg.InputQueue)
                .ConfigureProperty("NumberOfWorkerThreads", cfg.NumberOfWorkerThreads)
                .ConfigureProperty("ErrorQueue", cfg.ErrorQueue)
                .ConfigureProperty("MaxRetries", cfg.MaxRetries);
        }

        private readonly IComponentConfig config;


        public ConfigMsmqTransport IsTransactional(bool value)
        {
            config.ConfigureProperty("IsTransactional", value);
            return this;
        }

        public ConfigMsmqTransport PurgeOnStartup(bool value)
        {
            config.ConfigureProperty("PurgeOnStartup", value);
            return this;
        }
    }
}
