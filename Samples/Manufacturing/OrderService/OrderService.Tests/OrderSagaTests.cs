using System;
using System.Collections.Generic;
using HR.Messages;
using NServiceBus.Saga;
using NServiceBus.Testing;
using OrderService.Messages;
using OrderLine=OrderService.Messages.OrderLine;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace OrderService.Tests
{
    [TestClass]
    public class OrderSagaTests
    {
        private OrderSaga orderSaga;
        private Saga Saga;
        string partnerAddress;
        Guid productId;
        float quantity;
        Guid partnerId;
        string purchaseOrderNumber;
        List<OrderLine> orderLines;


        [TestInitialize]
        public void Setup()
        {
            Saga = Saga.Test(out orderSaga);

            partnerAddress = "partner";
            productId = Guid.NewGuid();
            quantity = 10.0F;
            partnerId = Guid.NewGuid();
            purchaseOrderNumber = Guid.NewGuid().ToString();
            orderLines = new List<OrderLine>();
            orderLines.Add(new OrderLine(productId, quantity));

        }

        [TestMethod]
        public void OrderSagaTest()
        {
            OrderMessage message = new OrderMessage(purchaseOrderNumber, partnerId, true, DateTime.Now, orderLines);

            Saga.WhenReceivesMessageFrom(partnerAddress)
                .ExpectSendToDestination<OrderStatusChangedMessage>(
                delegate(string destination, OrderStatusChangedMessage m)
                    {
                        return (destination == partnerAddress && Check(m, OrderStatusEnum.Recieved));
                    }
                )
                .ExpectPublish<OrderStatusChangedMessage>(
                delegate(OrderStatusChangedMessage m)
                    {
                        return Check(m, OrderStatusEnum.Recieved);
                    }
                )
                .ExpectSend<RequestOrderAuthorizationMessage>(
                delegate(RequestOrderAuthorizationMessage m)
                    {
                        return (
                                   m.PartnerId == partnerId &&
                                   m.OrderLines.Count == 1 &&
                                   m.OrderLines[0].ProductId == productId &&
                                   m.OrderLines[0].Quantity == quantity
                               );
                    }
                )
                .When(delegate { orderSaga.Handle(message); });

            List<HR.Messages.OrderLine> hrLines = new List<HR.Messages.OrderLine>(1);
            hrLines.Add(new HR.Messages.OrderLine(productId, quantity));

            Saga.ExpectSendToDestination<OrderStatusChangedMessage>(
                delegate(string destination, OrderStatusChangedMessage m)
                {
                    return (destination == partnerAddress && Check(m, OrderStatusEnum.Accepted));
                }
                )
                .ExpectPublish<OrderStatusChangedMessage>(
                delegate(OrderStatusChangedMessage m)
                {
                    return Check(m, OrderStatusEnum.Accepted);
                }
                )
            .When(delegate { orderSaga.Handle(new OrderAuthorizationResponseMessage(orderSaga.Entity.Id, true, hrLines)); });
        }

        [TestMethod]
        public void TimeoutTest()
        {
            OrderMessage message = new OrderMessage(purchaseOrderNumber, partnerId, true, DateTime.Now, orderLines);
            object state = null;

            Saga.WhenReceivesMessageFrom(partnerAddress)
                .ExpectSendToDestination<OrderStatusChangedMessage>(
                delegate(string destination, OrderStatusChangedMessage m)
                {
                    return (destination == partnerAddress && Check(m, OrderStatusEnum.Recieved));
                }
                )
                .ExpectPublish<OrderStatusChangedMessage>(
                delegate(OrderStatusChangedMessage m)
                {
                    return Check(m, OrderStatusEnum.Recieved);
                }
                )
                .ExpectSend<RequestOrderAuthorizationMessage>(
                delegate(RequestOrderAuthorizationMessage m)
                {
                    return (
                               m.PartnerId == partnerId &&
                               m.OrderLines.Count == 1 &&
                               m.OrderLines[0].ProductId == productId &&
                               m.OrderLines[0].Quantity == quantity
                           );
                }
                )
                .ExpectSend<TimeoutMessage>(
                delegate(TimeoutMessage m)
                    {
                        state = m.State;

                        return m.SagaId == orderSaga.Entity.Id;
                    }
                )
                .When(delegate { orderSaga.Handle(message); });

            Saga.ExpectSendToDestination<OrderStatusChangedMessage>(
                delegate(string destination, OrderStatusChangedMessage m)
                {
                    return (destination == partnerAddress && BasicCheck(m, OrderStatusEnum.Accepted));
                }
                )
                .ExpectPublish<OrderStatusChangedMessage>(
                delegate(OrderStatusChangedMessage m)
                {
                    return BasicCheck(m, OrderStatusEnum.Accepted);
                }
                )
            .When(delegate { orderSaga.Timeout(state); });

            Assert.IsTrue(orderSaga.Completed);

        }

        private bool Check(OrderStatusChangedMessage m, OrderStatusEnum status)
        {
            return (
                       m.PartnerId == partnerId &&
                       m.PurchaseOrderNumber == purchaseOrderNumber &&
                       m.Status == status &&
                       m.OrderLines.Count == 1 &&
                       m.OrderLines[0].ProductId == productId &&
                       m.OrderLines[0].Quantity == quantity
                   );
        }

        private bool BasicCheck(OrderStatusChangedMessage m, OrderStatusEnum status)
        {
            return (
                       m.PartnerId == partnerId &&
                       m.PurchaseOrderNumber == purchaseOrderNumber &&
                       m.Status == status
                   );
        }

    }
}
