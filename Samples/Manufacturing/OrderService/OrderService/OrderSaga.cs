using System;
using System.Collections.Generic;
using HR.Messages;
using NServiceBus.Saga;
using OrderService.Messages;
using NServiceBus;

namespace OrderService
{
    public class OrderSaga : Saga<OrderSagaData>,
        ISagaStartedBy<OrderMessage>,
        IMessageHandler<OrderAuthorizationResponseMessage>,
        IMessageHandler<CancelOrderMessage>
    {
        public void Handle(OrderMessage message)
        {
            this.Data.PurchaseOrderNumber = message.PurchaseOrderNumber;
            this.Data.PartnerId = message.PartnerId;
            this.Data.ProvideBy = message.ProvideBy;

            foreach (Messages.OrderLine ol in message.OrderLines)
                this.Data.UpdateOrderLine(ol.ProductId, ol.Quantity);

            OrderStatusChangedMessage m = new OrderStatusChangedMessage(this.Data.PurchaseOrderNumber, this.Data.PartnerId, OrderStatusEnum.Recieved, GetOrderLines(this.Data.Lines));

            if (message.Done)
            {
                this.ReplyToOriginator(m);
                this.Bus.Publish(m);

                this.Bus.Send(new RequestOrderAuthorizationMessage(this.Data.Id, this.Data.PartnerId, Convert<Messages.OrderLine, HR.Messages.OrderLine>(m.OrderLines)));

                this.RequestTimeout(this.Data.ProvideBy - TimeSpan.FromSeconds(2), null);
            }
            else
            {
                m.Status = OrderStatusEnum.Tentative;
                this.Bus.Publish(m);
            }
        }

        public void Handle(OrderAuthorizationResponseMessage message)
        {
            OrderStatusChangedMessage m = new OrderStatusChangedMessage(this.Data.PurchaseOrderNumber, this.Data.PartnerId, (message.Success ? OrderStatusEnum.Authorized : OrderStatusEnum.Rejected), Convert<HR.Messages.OrderLine, Messages.OrderLine>(message.OrderLines));
            
            this.ReplyToOriginator(m);
            this.Bus.Publish(m);

            foreach (HR.Messages.OrderLine ol in message.OrderLines)
                this.Data.UpdateAuthorization(message.Success, ol.ProductId, ol.Quantity);

            if (this.Data.IsAuthorized)
                Complete();
        }

        public void Handle(CancelOrderMessage message)
        {

        }

        private void Complete()
        {
            OrderStatusChangedMessage finalStatus =
                new OrderStatusChangedMessage(this.Data.PurchaseOrderNumber, this.Data.PartnerId, OrderStatusEnum.Accepted,
                                              GetOrderLines(this.Data.Lines));
            this.Bus.Publish(finalStatus);
            this.ReplyToOriginator(finalStatus);

            this.MarkAsComplete();
        }

        private static List<K> Convert<T, K>(List<T> list) where T : IOrderLine where K : IOrderLine, new()
        {
            List<K> result = new List<K>(list.Count);

            list.ForEach(
                delegate(T ol)
                    {
                        K k = new K();
                        k.ProductId = ol.ProductId;
                        k.Quantity = ol.Quantity;

                        result.Add(k);
                    }
                );

            return result;
        }

        private static List<Messages.OrderLine> GetOrderLines(IEnumerable<OrderLine> lines)
        {
            List<Messages.OrderLine> result = new List<Messages.OrderLine>();

            foreach(OrderLine ol in lines)
                result.Add(new Messages.OrderLine(ol.ProductId, ol.Quantity));

            return result;
        }

        public override void Timeout(object state)
        {
            this.Complete();
        }
    }
}
