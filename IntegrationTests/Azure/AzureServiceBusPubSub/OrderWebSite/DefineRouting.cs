using NServiceBus.Config;
using NServiceBus.Config.ConfigurationSource;

namespace OrderWebSite
{
    class DefineRouting : IProvideConfiguration<UnicastBusConfig>
    {
        public UnicastBusConfig GetConfiguration()
        {
            return new UnicastBusConfig {
                MessageEndpointMappings = new MessageEndpointMappingCollection
                {
                    new MessageEndpointMapping { Messages="MyMessages", Endpoint="orderservice" }
                }
            };
        }
    }
}