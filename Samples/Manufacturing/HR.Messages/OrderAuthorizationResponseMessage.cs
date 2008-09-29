using System;
using System.Collections.Generic;
using NServiceBus.Saga;

namespace HR.Messages
{
    [Serializable]
    public class OrderAuthorizationResponseMessage : ISagaMessage
    {
         public OrderAuthorizationResponseMessage(Guid sagaId, bool success, List<OrderLine> orderLines)
        {
            this.sagaId = sagaId;
            this.Success = success;
            this.OrderLines = (orderLines ?? new List<OrderLine>());
        }

        public OrderAuthorizationResponseMessage()
        {
            
        }

        public Guid SagaId
        {
            get { return sagaId; }
            set { sagaId = value; }
        }

        private Guid sagaId;

        public bool Success;

        public List<OrderLine> OrderLines;
    }
}
