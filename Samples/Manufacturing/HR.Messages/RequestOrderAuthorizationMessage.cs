using System;
using NServiceBus.Saga;
using System.Collections.Generic;

namespace HR.Messages
{
    [Serializable]
    public class RequestOrderAuthorizationMessage : ISagaMessage
    {
        public RequestOrderAuthorizationMessage(Guid sagaId, Guid partnerId, List<OrderLine> orderLines)
        {
            this.sagaId = sagaId;
            this.PartnerId = partnerId;
            this.OrderLines = (orderLines ?? new List<OrderLine>());
        }

        public RequestOrderAuthorizationMessage()
        {
            
        }

        public Guid SagaId
        {
            get { return sagaId; }
            set { sagaId = value; }
        }

        private Guid sagaId;

        public Guid PartnerId;

        public List<OrderLine> OrderLines;
    }
}
