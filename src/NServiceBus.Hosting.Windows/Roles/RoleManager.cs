namespace NServiceBus
{
    using System.Linq;
    using NServiceBus.Features;

    class RoleManager
    {

        public static void ConfigureBusForEndpoint(IConfigureThisEndpoint specifier,Configure config)
        {
            if (specifier is AsA_Server)
            {
                config.ScaleOut(s => s.UseSingleBrokerQueue());
            }
            else if (specifier is AsA_Client)
            {
                config.DisableFeature<Features.SecondLevelRetries>()
                    .DisableFeature<StorageDrivenPublishing>()
                    .DisableFeature<TimeoutManager>()
                    .Transactions(t => t.Disable())
                    .PurgeOnStartup(true); 
            }

            var transportType = specifier.GetType()
                .GetInterfaces()
                .Where(x=>x.IsGenericType)
                .SingleOrDefault(x => x.GetGenericTypeDefinition() == typeof(UsingTransport<>));
            if (transportType == null)
            {
                return;
            }
            var transportDefinitionType = transportType.GetGenericArguments().First();

            config.UseTransport(transportDefinitionType);
        }

    }


}