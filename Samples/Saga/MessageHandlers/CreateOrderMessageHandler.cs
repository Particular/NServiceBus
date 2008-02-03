
using ExternalOrderMessages;
using NServiceBus;
using NServiceBus.Saga;
using OrderQueryLogic;
using ObjectBuilder;

namespace MessageHandlers
{
    public class CreateOrderMessageHandler : IMessageHandler<CreateOrderMessage>
    {
        public void Handle(CreateOrderMessage message)
        {
            ISaga<CreateOrderMessage> saga = this.query.Find<CreateOrderMessage>(message.OrderId);

            if (saga == null)
                saga = this.builer.Build(typeof (ISaga<CreateOrderMessage>)) as ISaga<CreateOrderMessage>;

            saga.Handle(message);        
        }

        private IQuerySagasByOrderIds query;
        public IQuerySagasByOrderIds Query
        {
            set { this.query = value; }
        }

        private IBuilder builer;
        public IBuilder Builder
        {
            set { this.builer = value; }
        }
    }
}
