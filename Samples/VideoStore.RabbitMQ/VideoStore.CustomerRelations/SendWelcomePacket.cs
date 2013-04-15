using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VideoStore.Messages.Commands;
using VideoStore.Messages.Events;
using NServiceBus;

namespace VideoStore.CustomerRelations
{
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
