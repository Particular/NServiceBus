using System.Configuration;
using ObjectBuilder;
using System.Collections;
using System.Reflection;

namespace NServiceBus.Unicast.Config
{
    public class ConfigUnicastBus
    {
        public ConfigUnicastBus(IBuilder builder)
        {
            UnicastBusConfig cfg = ConfigurationManager.GetSection("UnicastBusConfig") as UnicastBusConfig;

            if (cfg == null)
                throw new ConfigurationErrorsException("Could not find configuration section for UnicastBus.");

            Hashtable hashtable = new Hashtable();

            foreach (MessageEndpointMapping mapping in cfg.MessageEndpointMappings)
                hashtable[mapping.Messages] = mapping.Endpoint;

            config = builder.ConfigureComponent(typeof (UnicastBus), ComponentCallModelEnum.Singleton)
                .ConfigureProperty("DistributorControlAddress", cfg.DistributorControlAddress)
                .ConfigureProperty("DistributorDataAddress", cfg.DistributorDataAddress)
                .ConfigureProperty("MessageOwners", hashtable);
        }

        private readonly IComponentConfig config;

        public ConfigUnicastBus ImpersonateSender(bool value)
        {
            config.ConfigureProperty("ImpersonateSender", value);
            return this;
        }

        public ConfigUnicastBus SetMessageHandlersFromAssembliesInOrder(params Assembly[] assemblies)
        {
            config.ConfigureProperty("MessageHandlerAssemblies", new ArrayList(assemblies));
            return this;
        }

        public ConfigUnicastBus PropogateReturnAddressOnSend(bool value)
        {
            config.ConfigureProperty("PropogateReturnAddressOnSend", value);
            return this;
        }

        public ConfigUnicastBus ForwardReceivedMessagesTo(string  value)
        {
            config.ConfigureProperty("ForwardReceivedMessagesTo", value);
            return this;
        }
    }
}
