using System;
using System.Collections.Generic;
using System.Text;
using ExternalOrderMessages;
using NServiceBus.Saga;

namespace ProcessingLogic
{
    [Serializable]
    public class OrderSagaData : ISagaEntity
    {
        private Guid id;
        private string originator;

        public Guid Id
        {
            get { return id; }
            set { id = value; }
        }

        public string Originator
        {
            get { return originator; }
            set { originator = value; }
        }

        public Guid ExternalOrderId
        {
            get { return externalOrderId; }
            set { externalOrderId = value; }
        }

        public int NumberOfPendingAuthorizations
        {
            get { return numberOfPendingAuthorizations; }
            set { numberOfPendingAuthorizations = value; }
        }

        public List<CreateOrderMessage> OrderItems
        {
            get { return orderItems; }
            set { orderItems = value; }
        }


        private Guid externalOrderId;
        private int numberOfPendingAuthorizations = 2;
        private List<CreateOrderMessage> orderItems = new List<CreateOrderMessage>();

    }
}
