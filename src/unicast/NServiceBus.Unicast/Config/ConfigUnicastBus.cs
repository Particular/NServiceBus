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

            bus = builder.ConfigureComponent<UnicastBus>(ComponentCallModelEnum.Singleton);

            bus.DistributorControlAddress = cfg.DistributorControlAddress;
            bus.DistributorDataAddress = cfg.DistributorDataAddress;
            bus.MessageOwners = hashtable;
        }

        private readonly UnicastBus bus;

        public ConfigUnicastBus ImpersonateSender(bool value)
        {
            bus.ImpersonateSender = value;
            return this;
        }

        public ConfigUnicastBus SetMessageHandlersFromAssembliesInOrder(params Assembly[] assemblies)
        {
            bus.MessageHandlerAssemblies = new ArrayList(assemblies);
            return this;
        }

        public ConfigUnicastBus PropogateReturnAddressOnSend(bool value)
        {
            bus.PropogateReturnAddressOnSend = value;
            return this;
        }

        public ConfigUnicastBus ForwardReceivedMessagesTo(string  value)
        {
            bus.ForwardReceivedMessagesTo = value;
            return this;
        }
    }
}
