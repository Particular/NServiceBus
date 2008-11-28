using System;
using System.Collections.Generic;
using HR.Messages;
using NServiceBus.Saga;
using NServiceBus.Testing;
using OrderService.Messages;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace OrderService.Tests
{
    [TestClass]
    public class OrderSagaTests
    {
        #region members

        private OrderSaga orderSaga;
        private Saga Saga;
        string partnerAddress;
        Guid productId;
        float quantity;
        Guid partnerId;
        string purchaseOrderNumber;
        List<Messages.OrderLine> orderLines;

        #endregion

        [TestInitialize]
        public void Setup()
        {
            Saga = Saga.Test(out orderSaga);

            partnerAddress = "partner";
            productId = Guid.NewGuid();
            quantity = 10.0F;
            partnerId = Guid.NewGuid();
            purchaseOrderNumber = Guid.NewGuid().ToString();
            orderLines = new List<Messages.OrderLine>();
            orderLines.Add(new Messages.OrderLine(productId, quantity));

        }

        [TestMethod]
        public void OrderSagaTest()
        {
            Saga.WhenReceivesMessageFrom(partnerAddress)
                .ExpectSendToDestination<OrderStatusChangedMessage>(
                    (dest, m) => (dest == partnerAddress && Check(m, OrderStatusEnum.Recieved)))
                .ExpectPublish<OrderStatusChangedMessage>(m => Check(m, OrderStatusEnum.Recieved))
                .ExpectSend<RequestOrderAuthorizationMessage>(m => Check(m))
                .When(() => orderSaga.Handle(CreateRequest()));

            Saga.ExpectSendToDestination<OrderStatusChangedMessage>(
                    (dest, m) => (dest == partnerAddress && Check(m, OrderStatusEnum.Accepted)))
                .ExpectPublish<OrderStatusChangedMessage>(m => Check(m, OrderStatusEnum.Accepted))
            .When(() => orderSaga.Handle(CreateResponse()));
        }

        [TestMethod]
        public void TimeoutTest()
        {
            object state = null;

            Saga.WhenReceivesMessageFrom(partnerAddress)
                .ExpectSendToDestination<OrderStatusChangedMessage>(
                    (dest, m) => (dest == partnerAddress && Check(m, OrderStatusEnum.Recieved)))
                .ExpectPublish<OrderStatusChangedMessage>(m => Check(m, OrderStatusEnum.Recieved))
                .ExpectSend<RequestOrderAuthorizationMessage>(m => Check(m))
                .ExpectSend<TimeoutMessage>(m => { state = m.State; return m.SagaId == orderSaga.Entity.Id; })
                .When(() => orderSaga.Handle(CreateRequest()));

            Saga.ExpectSendToDestination<OrderStatusChangedMessage>(
                    (dest, m) => (dest == partnerAddress && Check(m, OrderStatusEnum.Accepted)))
                .ExpectPublish<OrderStatusChangedMessage>(m => BasicCheck(m, OrderStatusEnum.Accepted))
            .When(() => orderSaga.Timeout(state));

            Assert.IsTrue(orderSaga.Completed);

        }

        #region helper methods

        private OrderMessage CreateRequest()
        {
            return new OrderMessage(purchaseOrderNumber, partnerId, true, DateTime.Now, orderLines);
        }

        private OrderAuthorizationResponseMessage CreateResponse()
        {
            List<HR.Messages.OrderLine> hrLines = new List<HR.Messages.OrderLine>(1);
            hrLines.Add(new HR.Messages.OrderLine(productId, quantity));

            return new OrderAuthorizationResponseMessage(
                orderSaga.Entity.Id, 
                true, 
                hrLines);
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

        private bool Check(RequestOrderAuthorizationMessage m)
        {
            return (
                       m.PartnerId == partnerId &&
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
        #endregion

    }
}
