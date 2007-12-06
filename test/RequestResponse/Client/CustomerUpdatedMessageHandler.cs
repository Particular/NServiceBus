using System;
using System.Collections.Generic;
using System.Text;
using Messages;
using NServiceBus;

namespace Client
{
    public class CustomerUpdatedMessageHandler : BaseMessageHandler<CustomerUpdatedMessage>
    {
        #region IMessageHandler<CustomerUpdatedMessage> Members

        public override void Handle(CustomerUpdatedMessage message)
        {
            Console.WriteLine("Customer {0} updated.", message.CustomerId.ToString());
        }

        #endregion
    }
}
