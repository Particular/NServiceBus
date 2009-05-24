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
        public override void ConfigureHowToFindSaga()
        {
            ConfigureMapping<OrderMessage>(s => s.PurchaseOrderNumber, m => m.PurchaseOrderNumber);
            ConfigureMapping<CancelOrderMessage>(s => s.PurchaseOrderNumber, m => m.PurchaseOrderNumber);
        }

        public void Handle(OrderMessage message)
        {
            this.Data.PurchaseOrderNumber = message.PurchaseOrderNumber;
            this.Data.PartnerId = message.PartnerId;
            this.Data.ProvideBy = message.ProvideBy;

            foreach (Messages.OrderLine ol in message.OrderLines)
                this.Data.UpdateOrderLine(ol.ProductId, ol.Quantity);

            var status = GetStatus(OrderStatusEnum.Recieved, GetOrderLines(this.Data.Lines));

            if (message.Done)
            {
                this.ReplyToOriginator(status);
                this.Bus.Publish(status);

                this.Bus.Send<RequestOrderAuthorizationMessage>(m => { m.SagaId = this.Data.Id; m.PartnerId = this.Data.PartnerId; m.OrderLines = Convert<Messages.OrderLine, HR.Messages.IOrderLine>(status.OrderLines); });

                this.RequestTimeout(this.Data.ProvideBy - TimeSpan.FromSeconds(2), null);
            }
            else
            {
                status.Status = OrderStatusEnum.Tentative;
                this.Bus.Publish(status);
            }
        }

        public void Handle(OrderAuthorizationResponseMessage message)
        {
            var status = GetStatus(
                (message.Success ? OrderStatusEnum.Authorized : OrderStatusEnum.Rejected),
                GetOrderLines(message.OrderLines));

            this.ReplyToOriginator(status);
            this.Bus.Publish(status);

            foreach (HR.Messages.IOrderLine ol in message.OrderLines)
                this.Data.UpdateAuthorization(message.Success, ol.ProductId, ol.Quantity);

            if (this.Data.IsAuthorized)
                Complete();
        }

        public void Handle(CancelOrderMessage message)
        {

        }

        private void Complete()
        {
            var finalStatus = GetStatus(OrderStatusEnum.Accepted, GetOrderLines(this.Data.Lines));

            this.Bus.Publish(finalStatus);
            this.ReplyToOriginator(finalStatus);

            this.MarkAsComplete();
        }

        public override void Timeout(object state)
        {
            this.Complete();
        }

        private List<K> Convert<T, K>(List<T> list) where T : Messages.OrderLine where K : IOrderLine
        {
            var result = new List<K>(list.Count);

            list.ForEach(ol => result.Add(this.Bus.CreateInstance<K>(k => { k.ProductId = ol.ProductId; k.Quantity = ol.Quantity; })));

            return result;
        }

        private static List<Messages.OrderLine> GetOrderLines(IEnumerable<OrderLine> lines)
        {
            var result = new List<Messages.OrderLine>();

            foreach (OrderLine ol in lines)
                result.Add<Messages.OrderLine>(o => { o.ProductId = ol.ProductId; o.Quantity = ol.Quantity; });

            return result;
        }

        private OrderStatusChangedMessage GetStatus(OrderStatusEnum status, List<Messages.OrderLine> lines)
        {
            return this.Bus.CreateInstance<OrderStatusChangedMessage>(m =>
            {
                m.PurchaseOrderNumber = this.Data.PurchaseOrderNumber;
                m.PartnerId = this.Data.PartnerId;
                m.Status = status;
                m.OrderLines = lines;
            });
        }

        private static List<Messages.OrderLine> GetOrderLines(IEnumerable<IOrderLine> lines)
        {
            var result = new List<Messages.OrderLine>();

            foreach (IOrderLine ol in lines)
                result.Add<Messages.OrderLine>(o => { o.ProductId = ol.ProductId; o.Quantity = ol.Quantity; });

            return result;
        }
    }
}
