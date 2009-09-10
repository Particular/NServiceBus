using System;
using System.Collections.Generic;
using HR.Messages;
using NServiceBus.Saga;
using OrderService.Messages;
using NServiceBus;

namespace OrderService
{
    public class OrderSaga : Saga<OrderSagaData>,
        IAmStartedByMessages<OrderMessage>,
        IHandleMessages<OrderAuthorizationResponseMessage>,
        IHandleMessages<CancelOrderMessage>
    {
        public override void ConfigureHowToFindSaga()
        {
            ConfigureMapping<OrderMessage>(s => s.PurchaseOrderNumber, m => m.PurchaseOrderNumber);
            ConfigureMapping<CancelOrderMessage>(s => s.PurchaseOrderNumber, m => m.PurchaseOrderNumber);
        }

        public void Handle(OrderMessage message)
        {
            Data.PurchaseOrderNumber = message.PurchaseOrderNumber;
            Data.PartnerId = message.PartnerId;
            Data.ProvideBy = message.ProvideBy;

            foreach (Messages.OrderLine ol in message.OrderLines)
                Data.UpdateOrderLine(ol.ProductId, ol.Quantity);

            var status = GetStatus(OrderStatusEnum.Recieved, GetOrderLines(Data.OrderLines));

            if (message.Done)
            {
                ReplyToOriginator(status);
                Bus.Publish(status);

                Bus.Send<RequestOrderAuthorizationMessage>(m => { m.SagaId = Data.Id; m.PartnerId = Data.PartnerId; m.OrderLines = Convert<Messages.OrderLine, IOrderLine>(status.OrderLines); });

                RequestTimeout(Data.ProvideBy - TimeSpan.FromSeconds(2), "state");
            }
            else
            {
                status.Status = OrderStatusEnum.Tentative;
                Bus.Publish(status);
            }
        }

        public void Handle(OrderAuthorizationResponseMessage message)
        {
            var status = GetStatus(
                (message.Success ? OrderStatusEnum.Authorized : OrderStatusEnum.Rejected),
                GetOrderLines(message.OrderLines));

            ReplyToOriginator(status);
            Bus.Publish(status);

            foreach (var ol in message.OrderLines)
                Data.UpdateAuthorization(message.Success, ol.ProductId, ol.Quantity);

            if (Data.IsAuthorized)
                Complete();
        }

        public void Handle(CancelOrderMessage message)
        {

        }

        private void Complete()
        {
            var finalStatus = GetStatus(OrderStatusEnum.Accepted, GetOrderLines(Data.OrderLines));

            Bus.Publish(finalStatus);
            ReplyToOriginator(finalStatus);

            MarkAsComplete();
        }

        public override void Timeout(object state)
        {
            Complete();
        }

        private List<K> Convert<T, K>(List<T> list) where T : Messages.OrderLine where K : IOrderLine
        {
            var result = new List<K>(list.Count);

            list.ForEach(ol => result.Add(Bus.CreateInstance<K>(k => { k.ProductId = ol.ProductId; k.Quantity = ol.Quantity; })));

            return result;
        }

        private static List<Messages.OrderLine> GetOrderLines(IEnumerable<OrderLine> lines)
        {
            var result = new List<Messages.OrderLine>();

            foreach (OrderLine ol in lines)
                result.Add(o => { o.ProductId = ol.ProductId; o.Quantity = ol.Quantity; });

            return result;
        }

        private OrderStatusChangedMessage GetStatus(OrderStatusEnum status, List<Messages.OrderLine> lines)
        {
            return Bus.CreateInstance<OrderStatusChangedMessage>(m =>
            {
                m.PurchaseOrderNumber = Data.PurchaseOrderNumber;
                m.PartnerId = Data.PartnerId;
                m.Status = status;
                m.OrderLines = lines;
            });
        }

        private static List<Messages.OrderLine> GetOrderLines(IEnumerable<IOrderLine> lines)
        {
            var result = new List<Messages.OrderLine>();

            foreach (IOrderLine ol in lines)
                result.Add(o => { o.ProductId = ol.ProductId; o.Quantity = ol.Quantity; });

            return result;
        }
    }
}
