using System;
using MessageMutators;
using Messages;
using NServiceBus;

namespace Server
{
    public class EndpointConfig : IConfigureThisEndpoint, AsA_Server {}

    public class ConfigureMutators : IWantCustomInitialization
    {
        public void Init()
        {
            Configure.Instance.Configurer.ConfigureComponent<ValidationMessageMutator>(
                DependencyLifecycle.InstancePerCall);
            Configure.Instance.Configurer.ConfigureComponent<TransportMessageCompressionMutator>(
                DependencyLifecycle.InstancePerCall);
        }
    }

    public class Handler : IHandleMessages<CreateProductCommand>
    {
        public void Handle(CreateProductCommand createProductCommand)
        {
            Console.WriteLine("Received a CreateProductCommand message: " + createProductCommand);
        }
    }
}
