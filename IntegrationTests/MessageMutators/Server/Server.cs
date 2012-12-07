using System;
using Messages;
using NServiceBus;

namespace Server
{
    public class EndpointConfig : IConfigureThisEndpoint, AsA_Server {}

    public class Handler : IHandleMessages<CreateProductCommand>
    {
        public void Handle(CreateProductCommand createProductCommand)
        {
            Console.WriteLine("Received a CreateProductCommand message: " + createProductCommand);
        }
    }
}
