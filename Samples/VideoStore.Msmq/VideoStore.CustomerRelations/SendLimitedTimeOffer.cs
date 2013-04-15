using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VideoStore.Messages.Events;
using NServiceBus;

namespace VideoStore.CustomerRelations
{
    class SendLimitedTimeOffer : IHandleMessages<ClientBecamePreferred>
    {
        public void Handle(ClientBecamePreferred message)
        {
            Console.WriteLine("Handler WhenCustomerIsPreferredSendLimitedTimeOffer invoked for CustomerId: {0}", message.ClientId);
        }
    }
}
