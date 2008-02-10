using System;
using System.Collections.Generic;
using ExternalOrderMessages;
using NServiceBus.Saga;
using NServiceBus;
using InternalOrderMessages;


namespace ProcessingLogic
{
    [Serializable]
    public class OrderSaga : ISaga<CreateOrderMessage>,
        ISaga<AuthorizeOrderResponseMessage>,
        ISaga<CancelOrderMessage>
    {
        #region config info

        [NonSerialized]
        private IBus bus;
        public IBus Bus
        {
            set { this.bus = value; }
        }

        #endregion

        private Guid id;
        private bool completed;
        public string clientAddress;
        public Guid externalOrderId;
        public int numberOfPendingAuthorizations = 2;
        public List<CreateOrderMessage> orderItems = new List<CreateOrderMessage>();

        public void Handle(CreateOrderMessage message)
        {
            this.clientAddress = this.bus.SourceOfMessageBeingHandled;
            this.externalOrderId = message.OrderId;

            this.orderItems.Add(message);

            if (message.Completed)
            {
                for (int i = 0; i < this.numberOfPendingAuthorizations; i++)
                {
                    AuthorizeOrderRequestMessage req = new AuthorizeOrderRequestMessage();
                    req.SagaId = this.id;
                    req.OrderData = orderItems;

                    this.bus.Send(req);
                }
            }

            this.SendUpdate(OrderStatus.Recieved);

            this.bus.Send(new TimeoutMessage(message.ProvideBy, this, null));
        }

        public void Timeout(object state)
        {
            if (this.numberOfPendingAuthorizations <= 1)
                this.Complete();
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

        public void Handle(AuthorizeOrderResponseMessage message)
        {
            if (message.Authorized)
            {
                this.numberOfPendingAuthorizations--;

                if (this.numberOfPendingAuthorizations == 1)
                    this.SendUpdate(OrderStatus.Authorized1);
                else
                {
                    this.SendUpdate(OrderStatus.Authorized2);
                    this.Complete();
                }
            }
            else
            {
                this.SendUpdate(OrderStatus.Rejected);
                this.Complete();
            }
        }

        public void Handle(CancelOrderMessage message)
        {

        }

        private void SendUpdate(OrderStatus status)
        {
            OrderStatusUpdatedMessage update = new OrderStatusUpdatedMessage();
            update.OrderId = this.externalOrderId;
            update.Status = status;

            this.bus.Send(this.clientAddress, update);
        }

        private void Complete()
        {
            this.completed = true;

            this.SendUpdate(OrderStatus.Accepted);

            OrderAcceptedMessage accepted = new OrderAcceptedMessage();
            accepted.Products = new List<Guid>(this.orderItems.Count);
            accepted.Amounts = new List<float>(this.orderItems.Count);

            this.orderItems.ForEach(delegate(CreateOrderMessage m)
                                        {
                                            accepted.Products.AddRange(m.Products);
                                            accepted.Amounts.AddRange(m.Amounts);
                                            accepted.CustomerId = m.CustomerId;
                                        });

            this.bus.Publish(accepted);
        }
    }
}
