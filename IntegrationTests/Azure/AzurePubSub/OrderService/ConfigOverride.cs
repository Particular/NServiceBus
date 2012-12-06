using NServiceBus.Config;
using NServiceBus.Config.ConfigurationSource;

namespace OrderService
{
    class ConfigOverride : IProvideConfiguration<UnicastBusConfig>
    {
        public UnicastBusConfig GetConfiguration()
        {
            return new UnicastBusConfig
            {
                MessageEndpointMappings = new MessageEndpointMappingCollection
                {
                    new MessageEndpointMapping { Messages="MyMessages", Endpoint="orderserviceinputqueue" }
                }
            };
        }
    }
}