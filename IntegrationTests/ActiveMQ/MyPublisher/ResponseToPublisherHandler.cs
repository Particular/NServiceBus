namespace MyPublisher
{
    using System;
    using MyMessages.Publisher;
    using NServiceBus;

    public class ResponseToPublisherHandler : IHandleMessages<ResponseToPublisher>
    {
        public void Handle(ResponseToPublisher message)
        {
            Console.WriteLine("Received response Message: {0}", message.ResponseId);
        }
    }
}