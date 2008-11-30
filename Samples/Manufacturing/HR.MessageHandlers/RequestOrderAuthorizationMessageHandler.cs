using HR.Messages;
using NServiceBus;
using System.Threading;

namespace HR.MessageHandlers
{
    public class RequestOrderAuthorizationMessageHandler : IMessageHandler<RequestOrderAuthorizationMessage>
    {
        public void Handle(RequestOrderAuthorizationMessage message)
        {
            if (message.OrderLines != null)
                foreach(IOrderLine ol in message.OrderLines)
                    if (ol.Quantity > 50F)
                        Thread.Sleep(10000);

            var response = this.bus.CreateInstance<OrderAuthorizationResponseMessage>(m => { m.SagaId = message.SagaId; m.Success = true; m.OrderLines = message.OrderLines; });

            this.bus.Reply(response);
        }

        private IBus bus;
        public IBus Bus
        {
            set { this.bus = value; }
        }
    }
}
