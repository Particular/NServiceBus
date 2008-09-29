using System;
using System.Collections.Generic;
using HR.Messages;
using NServiceBus.Saga;
using OrderService.Messages;
using NServiceBus;
using OrderLine=OrderService.Messages.OrderLine;

namespace OrderService
{
    public class OrderSaga : ISaga<OrderMessage>,
        ISaga<OrderAuthorizationResponseMessage>,
        ISaga<CancelOrderMessage>
    {
        private IBus bus;
        public IBus Bus
        {
            set { this.bus = value; }
        }

        private Guid id;
        private bool completed;
        private string purchaseOrderNumber;
        private Guid partnerId;
        private DateTime provideBy;
        private string partnerAddress;
        private Dictionary<Guid, float> orderData = new Dictionary<Guid, float>();
        private Dictionary<Guid, float> authorizedOrderData = new Dictionary<Guid, float>();


        public void Handle(OrderMessage message)
        {
            this.partnerAddress = this.bus.SourceOfMessageBeingHandled;
            this.purchaseOrderNumber = message.PurchaseOrderNumber;
            this.partnerId = message.PartnerId;
            this.provideBy = message.ProvideBy;

            foreach (OrderLine ol in message.OrderLines)
                orderData[ol.ProductId] = ol.Quantity;

            OrderStatusChangedMessage m = new OrderStatusChangedMessage(this.purchaseOrderNumber, this.PartnerId, OrderStatusEnum.Recieved, GetOrderLines(this.orderData));

            if (message.Done)
            {
                this.bus.Send(this.partnerAddress, m);
                this.bus.Publish(m);

                this.bus.Send(new RequestOrderAuthorizationMessage(this.id, this.PartnerId, Convert<OrderLine, HR.Messages.OrderLine>(m.OrderLines)));

                this.bus.Send(new TimeoutMessage(this.provideBy - TimeSpan.FromSeconds(2), this, null));
            }
            else
            {
                m.Status = OrderStatusEnum.Tentative;
                this.bus.Publish(m);
            }
        }

        public void Handle(OrderAuthorizationResponseMessage message)
        {
            OrderStatusChangedMessage m = new OrderStatusChangedMessage(this.purchaseOrderNumber, this.PartnerId, (message.Success ? OrderStatusEnum.Authorized : OrderStatusEnum.Rejected), Convert<HR.Messages.OrderLine, OrderLine>(message.OrderLines));
            
            this.bus.Send(this.partnerAddress, m);
            this.bus.Publish(m);

            foreach (HR.Messages.OrderLine ol in message.OrderLines)
                if (message.Success)
                    this.authorizedOrderData[ol.ProductId] = ol.Quantity;
                else
                    this.orderData.Remove(ol.ProductId);

            if (this.authorizedOrderData.Count == this.orderData.Count)
                Complete();
        }

        public void Handle(CancelOrderMessage message)
        {

        }

        private void Complete()
        {
            OrderStatusChangedMessage finalStatus =
                new OrderStatusChangedMessage(this.purchaseOrderNumber, this.PartnerId, OrderStatusEnum.Accepted,
                                              GetOrderLines(authorizedOrderData));
            this.bus.Publish(finalStatus);
            this.bus.Send(this.partnerAddress, finalStatus);

            this.completed = true;
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

        private static List<OrderLine> GetOrderLines(Dictionary<Guid, float> data)
        {
            List<OrderLine> result = new List<OrderLine>(data.Count);

            foreach(Guid key in data.Keys)
                result.Add(new OrderLine(key, data[key]));

            return result;
        }

        public void Timeout(object state)
        {
            Complete();
        }

        public Guid Id
        {
            get { return id; }
            set { id = value; }
        }

        public bool Completed
        {
            get { return completed; }
        }

        public Guid PartnerId
        {
            get { return this.partnerId; }
        } 

        public string PurchaseOrderNumber
        {
            get { return this.purchaseOrderNumber; }
        }
    }
}
