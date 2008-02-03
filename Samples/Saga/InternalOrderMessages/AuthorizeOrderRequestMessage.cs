using System;
using System.Collections.Generic;
using NServiceBus;
using NServiceBus.Saga;
using ExternalOrderMessages;

namespace InternalOrderMessages
{
    [Serializable]
    public class AuthorizeOrderRequestMessage : ISagaMessage
    {
        public Guid SagaId
        {
            get { return sagaId; }
            set { sagaId = value; }
        }

        private Guid sagaId;

        public List<CreateOrderMessage> OrderData;
    }
}
