using System;
using System.Threading.Tasks;
using NServiceBus;
using NServiceBus.Features;

namespace EndpointB
{
    using NServiceBus.CentralizedRouting;

    static class Configuration
    {
        public static async Task Start(string discriminator)
        {
            var endpointConfiguration = new EndpointConfiguration("endpointB");
            endpointConfiguration.MakeInstanceUniquelyAddressable(discriminator);

            endpointConfiguration.UsePersistence<InMemoryPersistence>();
            endpointConfiguration.DisableFeature<AutoSubscribe>();
            endpointConfiguration.SendFailedMessagesTo("error");

            endpointConfiguration.EnableFeature<CentralizedRoutingFeature>();

            var endpoint = await Endpoint.Start(endpointConfiguration);

            Console.WriteLine("Press [Esc] to quit.");

            while (true)
            {
                var key = Console.ReadKey();
                if (key.Key == ConsoleKey.Escape)
                {
                    break;
                }
            }

            await endpoint.Stop();
        }
    }
}
