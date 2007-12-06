using NServiceBus;
using System;

namespace Messages
{
    [Serializable]
    public class UpdateCustomerMessage : IMessage
    {
        private Guid customerId;
        public Guid CustomerId
        {
            get { return customerId; }
            set { customerId = value; }
        }
    }

    [Serializable]
    public class CustomerUpdatedMessage : IMessage
    {
        private Guid customerId;
        public Guid CustomerId
        {
            get { return customerId; }
            set { customerId = value; }
        }

        // other customer data
    }
}
