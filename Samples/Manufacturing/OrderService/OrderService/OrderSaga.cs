using System;
using System.Collections.Generic;
using HR.Messages;
using NServiceBus.Saga;
using OrderService.Messages;
using NServiceBus;
using IOrderLine = HR.Messages.IOrderLine;

namespace OrderService
{
    public class OrderSaga : Saga<OrderSagaData>,
        IAmStartedByMessages<IOrderMessage>,
        IHandleMessages<OrderAuthorizationResponseMessage>,
        IHandleMessages<CancelOrderMessage>,
        IHandleTimeouts<DelayMessage>
    {
        public override void ConfigureHowToFindSaga()
        {
            ConfigureMapping<IOrderMessage>(s => s.PurchaseOrderNumber, m => m.PurchaseOrderNumber);
            ConfigureMapping<CancelOrderMessage>(s => s.PurchaseOrderNumber, m => m.PurchaseOrderNumber);
            // Notice that we have no mappings for the OrderAuthorizationResponseMessage message. This is not needed since the HR
            // endpoint will do a Bus.Reply and NServiceBus will then automatically correlate the reply back to
            // the originating saga
        }

        public void Handle(IOrderMessage message)
        {
            Console.WriteLine("Received message: " + message);
            
            Data.PurchaseOrderNumber = message.PurchaseOrderNumber;
            Data.PartnerId = message.PartnerId;
            Data.ProvideBy = message.ProvideBy;

            foreach (Messages.IOrderLine ol in message.OrderLines)
                Data.UpdateOrderLine(ol.ProductId, ol.Quantity);

            var status = GetStatus(OrderStatusEnum.Recieved, GetOrderLines(Data.OrderLines));

            if (message.Done)
            {
                ReplyToOriginator(status);
                Bus.Publish(status);

                Bus.Send<RequestOrderAuthorizationMessage>(m =>
                                                               {
                                                                   m.PartnerId = Data.PartnerId; 
                                                                   m.OrderLines = Convert<Messages.IOrderLine, IOrderLine>(status.OrderLines);
                                                               });

                RequestUtcTimeout<DelayMessage>(Data.ProvideBy - TimeSpan.FromSeconds(2), delayMessage => delayMessage.State = "state");
            }
            else
            {
                status.Status = OrderStatusEnum.Tentative;
                Console.WriteLine("Publishing: " + OrderStatusEnum.Tentative);
                Bus.Publish(status);
            }
        }

        public void Handle(OrderAuthorizationResponseMessage message)
        {
            Console.WriteLine("======================================================================");

            var status = GetStatus(
                (message.Success ? OrderStatusEnum.Authorized : OrderStatusEnum.Rejected),
                GetOrderLines(message.OrderLines));

            ReplyToOriginator(status);
            Bus.Publish(status);

            foreach (var ol in message.OrderLines)
                Data.UpdateAuthorization(message.Success, ol.ProductId, ol.Quantity);

            if (Data.IsAuthorized())
                Complete();
        }

        public void Handle(CancelOrderMessage message)
        {
            Console.WriteLine("======================================================================");

        }

        private void Complete()
        {
            var finalStatus = GetStatus(OrderStatusEnum.Accepted, GetOrderLines(Data.OrderLines));

            Bus.Publish(finalStatus);
            ReplyToOriginator(finalStatus);

            MarkAsComplete();
        }

        public void Timeout(DelayMessage state)
        {
            Console.WriteLine("======================================================================");

            Complete();
        }

        private List<K> Convert<T, K>(List<T> list) where T : Messages.IOrderLine where K : IOrderLine
        {
            var result = new List<K>(list.Count);

            list.ForEach(ol => result.Add(Bus.CreateInstance<K>(k => { k.ProductId = ol.ProductId; k.Quantity = ol.Quantity; })));

            return result;
        }

        private static List<Messages.IOrderLine> GetOrderLines(IEnumerable<OrderLine> lines)
        {
            var result = new List<Messages.IOrderLine>();

            foreach (OrderLine ol in lines)
                result.Add(o => { o.ProductId = ol.ProductId; o.Quantity = ol.Quantity; });

            return result;
        }

        private IOrderStatusChangedMessage GetStatus(OrderStatusEnum status, List<Messages.IOrderLine> lines)
        {
            return Bus.CreateInstance<IOrderStatusChangedMessage>(m =>
            {
                m.PurchaseOrderNumber = Data.PurchaseOrderNumber;
                m.PartnerId = Data.PartnerId;
                m.Status = status;
                m.OrderLines = lines;
            });
        }

        private static List<Messages.IOrderLine> GetOrderLines(IEnumerable<IOrderLine> lines)
        {
            var result = new List<Messages.IOrderLine>();

            foreach (IOrderLine ol in lines)
                result.Add(o => { o.ProductId = ol.ProductId; o.Quantity = ol.Quantity; });

            return result;
        }
    }
}
