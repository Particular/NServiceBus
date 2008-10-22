using System;
using System.Collections.Generic;
using ExternalOrderMessages;
using NServiceBus.Saga;
using NServiceBus;
using InternalOrderMessages;


namespace ProcessingLogic
{
    public class OrderSaga : Saga<OrderSagaData>,
        ISagaStartedBy<CreateOrderMessage>,
        IMessageHandler<AuthorizeOrderResponseMessage>,
        IMessageHandler<CancelOrderMessage>
    {
        public void Handle(CreateOrderMessage message)
        {
            this.Data.ExternalOrderId = message.OrderId;

            this.Data.OrderItems.Add(message);

            if (message.Completed)
            {
                for (int i = 0; i < this.Data.NumberOfPendingAuthorizations; i++)
                {
                    AuthorizeOrderRequestMessage req = new AuthorizeOrderRequestMessage();
                    req.SagaId = this.Data.Id;
                    req.OrderData = this.Data.OrderItems;

                    this.Bus.Send(req);
                }
            }

            this.SendUpdate(OrderStatus.Recieved);

            this.RequestTimeout(message.ProvideBy, null);
        }

        public override void Timeout(object state)
        {
            if (this.Data.NumberOfPendingAuthorizations <= 1)
                this.Complete();
        }

        public void Handle(AuthorizeOrderResponseMessage message)
        {
            if (message.Authorized)
            {
                this.Data.NumberOfPendingAuthorizations--;

                if (this.Data.NumberOfPendingAuthorizations == 1)
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
            update.OrderId = this.Data.ExternalOrderId;
            update.Status = status;

            this.ReplyToOriginator(update);
        }

        private void Complete()
        {
            this.MarkAsComplete();

            this.SendUpdate(OrderStatus.Accepted);

            OrderAcceptedMessage accepted = new OrderAcceptedMessage();
            accepted.Products = new List<Guid>(this.Data.OrderItems.Count);
            accepted.Amounts = new List<float>(this.Data.OrderItems.Count);

            this.Data.OrderItems.ForEach(delegate(CreateOrderMessage m)
                                        {
                                            accepted.Products.AddRange(m.Products);
                                            accepted.Amounts.AddRange(m.Amounts);
                                            accepted.CustomerId = m.CustomerId;
                                        });

            this.Bus.Publish(accepted);
        }
    }
}
