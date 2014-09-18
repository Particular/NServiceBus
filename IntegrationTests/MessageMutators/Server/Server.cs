using System;
using Messages;
using NServiceBus;

namespace Server
{
    public class EndpointConfig : IConfigureThisEndpoint, AsA_Server
    {
        public void Customize(BusConfiguration configuration)
        {
            configuration.UsePersistence<InMemoryPersistence>();
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
