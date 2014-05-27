namespace NServiceBus.Hosting.Roles.Handlers
{
    using System.Linq;
    using Transports;

    class TransportRoleHandler : IConfigureRole<UsingTransport<TransportDefinition>>
    {
        public void ConfigureRole(IConfigureThisEndpoint specifier,Configure config)
        {
            var transportDefinitionType =
                specifier.GetType()
                         .GetInterfaces()
                         .SelectMany(i => i.GetGenericArguments())
                         .Single(t => typeof (TransportDefinition).IsAssignableFrom(t));

            config.UseTransport(transportDefinitionType);
        }
    }
}