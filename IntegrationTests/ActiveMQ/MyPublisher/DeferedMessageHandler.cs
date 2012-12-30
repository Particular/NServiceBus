namespace MyPublisher
{
    using System;

    using MyMessages.Publisher;

    using NServiceBus;

    public class DeferedMessageHandler : IHandleMessages<DeferedMessage>
    {
        public IBus Bus { get; set; }

        public void Handle(DeferedMessage message)
        {
            Console.WriteLine("{0} - Deferred message with id {1} processed.", DateTime.Now.ToLongTimeString(), message.Id);
            Console.WriteLine("==========================================================================");
        }
    }
}