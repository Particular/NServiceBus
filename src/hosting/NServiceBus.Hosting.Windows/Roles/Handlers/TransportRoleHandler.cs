namespace NServiceBus.Hosting.Windows.Roles.Handlers
{
    using System.Linq;
    using Hosting.Roles;
    using Unicast.Config;
    using Unicast.Queuing;
    using Unicast.Transport;

    /// <summary>
    /// Configuring the right transport based on  UsingTransport<T> role on the endpoint config
    /// </summary>
    public class TransportRoleHandler : IConfigureRole<UsingTransport<ITransportDefinition>>, IWantToRunBeforeConfigurationIsFinalized
    {
        public ConfigUnicastBus ConfigureRole(IConfigureThisEndpoint specifier)
        {

            //get ITransportDefinition
            var transportDefinitionType =
                specifier.GetType()
                         .GetInterfaces()
                         .SelectMany(i => i.GetGenericArguments())
                         .Single(t => typeof(ITransportDefinition).IsAssignableFrom(t));

            Configure.Instance.UseTransport(transportDefinitionType);

            return null;
        }

        public void Run()
        {
            if (Configure.Instance.Configurer.HasComponent<ISendMessages>())
                return;

            Configure.Instance.UseTransport<Msmq>();
        }
    }
}