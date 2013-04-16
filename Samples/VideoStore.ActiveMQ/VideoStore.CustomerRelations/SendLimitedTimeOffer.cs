namespace VideoStore.CustomerRelations
{
    using System;
    using Messages.Events;
    using NServiceBus;

    class SendLimitedTimeOffer : IHandleMessages<ClientBecamePreferred>
    {
        public void Handle(ClientBecamePreferred message)
        {
            Console.WriteLine("Handler WhenCustomerIsPreferredSendLimitedTimeOffer invoked for CustomerId: {0}", message.ClientId);
        }
    }
}
