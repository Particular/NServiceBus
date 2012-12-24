namespace MyPublisher
{
    using System;

    using MyMessages.Publisher;

    using NServiceBus;

    public class LocalCommandMessageHandler : IHandleMessages<LocalCommand>
    {
        public void Handle(LocalCommand message)
        {
            Console.WriteLine("Received local command {0}.", message.CommandId);
        }
    }
}