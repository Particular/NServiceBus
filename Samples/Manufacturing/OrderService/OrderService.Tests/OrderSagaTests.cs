using System;
using System.Collections.Generic;
using HR.Messages;
using NServiceBus.Saga;
using NServiceBus.Testing;
using OrderService.Messages;
using NUnit.Framework;
using NServiceBus;
using IOrderLine = HR.Messages.IOrderLine;

namespace OrderService.Tests
{
    [TestFixture]
    public class OrderSagaTests
    {
        #region members

        string partnerAddress;
        Guid productId;
        float quantity;
        Guid partnerId;
        string purchaseOrderNumber;
        List<Messages.IOrderLine> orderLines;

        #endregion

        [TestFixtureSetUp]
        public void Setup()
        {
            Test.Initialize();

            partnerAddress = "partner";
            productId = Guid.NewGuid();
            quantity = 10.0F;
            partnerId = Guid.NewGuid();
            purchaseOrderNumber = Guid.NewGuid().ToString();
            orderLines = new List<Messages.IOrderLine>();
            orderLines.Add(ol => { ol.ProductId = productId; ol.Quantity = quantity; });
        }

        [Test]
        public void OrderSagaTest()
        {
            var sagaId = Guid.NewGuid();
            var order = CreateRequest();

            Test.Saga<OrderSaga>(sagaId).WhenReceivesMessageFrom(partnerAddress)
                .ExpectReplyToOrginator<IOrderStatusChangedMessage>(m => (Check(m, OrderStatusEnum.Recieved)))
                .ExpectPublish<IOrderStatusChangedMessage>(m => Check(m, OrderStatusEnum.Recieved))
                .ExpectSend<RequestOrderAuthorizationMessage>(Check)
                .ExpectTimeoutToBeSetAt<DelayMessage>((state, at) => at == order.ProvideBy - TimeSpan.FromSeconds(2) && state.State == "state")
            .When(os => os.Handle(order))
                .ExpectReplyToOrginator<IOrderStatusChangedMessage>(m => (Check(m, OrderStatusEnum.Accepted)))
                .ExpectPublish<IOrderStatusChangedMessage>(m => Check(m, OrderStatusEnum.Accepted))
            .When(os => os.Handle(CreateResponse()));
        }

        [Test]
        public void TimeoutTest()
        {
            var sagaId = Guid.NewGuid();

            Test.Saga<OrderSaga>(sagaId).WhenReceivesMessageFrom(partnerAddress)
                .ExpectReplyToOrginator<IOrderStatusChangedMessage>(m => (Check(m, OrderStatusEnum.Recieved)))
                .ExpectPublish<IOrderStatusChangedMessage>(m => Check(m, OrderStatusEnum.Recieved))
                .ExpectSend<RequestOrderAuthorizationMessage>(Check)
                .ExpectTimeoutToBeSetAt<DelayMessage>()
            .When(os => os.Handle(CreateRequest()))

                .ExpectReplyToOrginator<IOrderStatusChangedMessage>(m => (Check(m, OrderStatusEnum.Accepted)))
                .ExpectPublish<IOrderStatusChangedMessage>(m => BasicCheck(m, OrderStatusEnum.Accepted))
            .When(os => os.Timeout(new DelayMessage()))
                .AssertSagaCompletionIs(true);
        }

        #region helper methods

        private IOrderMessage CreateRequest()
        {
            return Test.CreateInstance<IOrderMessage>(m =>
            {
                m.PurchaseOrderNumber = purchaseOrderNumber;
                m.PartnerId = partnerId;
                m.Done = true;
                m.ProvideBy = DateTime.Now + TimeSpan.FromDays(2);
                m.OrderLines = orderLines;
            });
        }

        private OrderAuthorizationResponseMessage CreateResponse()
        {
            var hrLines = new List<IOrderLine>
                              {
                                  Test.CreateInstance<IOrderLine>(m =>
                                  {
                                      m.ProductId = productId;
                                      m.Quantity = quantity;
                                  })
                              };

            return Test.CreateInstance<OrderAuthorizationResponseMessage>(m => { m.Success = true; m.OrderLines = hrLines; });
        }

        private bool Check(IOrderStatusChangedMessage m, OrderStatusEnum status)
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

        private bool BasicCheck(IOrderStatusChangedMessage m, OrderStatusEnum status)
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
