using System;
using ExternalOrderMessages;
using NServiceBus;
using NServiceBus.Saga;
using OrderQueryLogic;

namespace MessageHandlers
{
    public class CancelOrderMessageHandler : IMessageHandler<CancelOrderMessage>
    {
        public void Handle(CancelOrderMessage message)
        {
            ISaga<CancelOrderMessage> saga = this.query.Find<CancelOrderMessage>(message.OrderId);
            saga.Handle(message);
        }

        private IQuerySagasByOrderIds query;
        public IQuerySagasByOrderIds Query
        {
            set { this.query = value; }
        }
    }
}
