namespace VideoStore.CustomerRelations
{
    using System;
    using Messages.Events;
    using NServiceBus;

    class SendWelcomePacket : IHandleMessages<ClientBecamePreferred>
    {
        public IBus Bus { get; set; }

        public void Handle(ClientBecamePreferred message)
        {
            Console.WriteLine("Handler WhenCustomerIsPreferredSendWelcomeEmail invoked for CustomerId: {0}", message.ClientId);

            // Don't write code to do the smtp send here, instead do a Bus.Send. If this handler fails, then 
            // the message to send email will not be sent.
            //Bus.Send<SendWelcomeEmail>(m => { m.ClientId = message.ClientId; });
        }
    }
}
