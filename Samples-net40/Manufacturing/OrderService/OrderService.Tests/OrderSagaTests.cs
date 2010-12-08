using System;
using System.Collections.Generic;
using HR.Messages;
using NServiceBus.Saga;
using NServiceBus.Testing;
using OrderService.Messages;
using NUnit.Framework;
using NServiceBus;

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
        List<Messages.OrderLine> orderLines;

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
            orderLines = new List<Messages.OrderLine>();
            orderLines.Add(ol => { ol.ProductId = productId; ol.Quantity = quantity; });
        }

        [Test]
        public void OrderSagaTest()
        {
            var sagaId = Guid.NewGuid();

            Test.Saga<OrderSaga>(sagaId).WhenReceivesMessageFrom(partnerAddress)
                .ExpectReplyToOrginator<OrderStatusChangedMessage>(m => (Check(m, OrderStatusEnum.Recieved)))
                .ExpectPublish<OrderStatusChangedMessage>(m => Check(m, OrderStatusEnum.Recieved))
                .ExpectSend<RequestOrderAuthorizationMessage>(Check)
                .ExpectSend<TimeoutMessage>(m => m.SagaId == sagaId)
            .When(os => os.Handle(CreateRequest()))

                .ExpectReplyToOrginator<OrderStatusChangedMessage>(m => (Check(m, OrderStatusEnum.Accepted)))
                .ExpectPublish<OrderStatusChangedMessage>(m => Check(m, OrderStatusEnum.Accepted))
            .When(os => os.Handle(CreateResponse(sagaId)));
        }

        [Test]
        public void TimeoutTest()
        {
            object state = null;
            var sagaId = Guid.NewGuid();

            Test.Saga<OrderSaga>(sagaId).WhenReceivesMessageFrom(partnerAddress)
                .ExpectReplyToOrginator<OrderStatusChangedMessage>(m => (Check(m, OrderStatusEnum.Recieved)))
                .ExpectPublish<OrderStatusChangedMessage>(m => Check(m, OrderStatusEnum.Recieved))
                .ExpectSend<RequestOrderAuthorizationMessage>(Check)
                .ExpectSend<TimeoutMessage>(m => { state = m.State; return m.SagaId == sagaId; })
            .When(os => os.Handle(CreateRequest()))

                .ExpectReplyToOrginator<OrderStatusChangedMessage>(m => (Check(m, OrderStatusEnum.Accepted)))
                .ExpectPublish<OrderStatusChangedMessage>(m => BasicCheck(m, OrderStatusEnum.Accepted))
            .When(os => os.Timeout(state))

            .AssertSagaCompletionIs(true);
        }

        #region helper methods

        private OrderMessage CreateRequest()
        {
            return Test.CreateInstance<OrderMessage>(m =>
            {
                m.PurchaseOrderNumber = purchaseOrderNumber;
                m.PartnerId = partnerId;
                m.Done = true;
                m.ProvideBy = DateTime.Now + TimeSpan.FromDays(2);
                m.OrderLines = orderLines;
            });
        }

        private OrderAuthorizationResponseMessage CreateResponse(Guid sagaId)
        {
            var hrLines = new List<IOrderLine>
                              {
                                  Test.CreateInstance<IOrderLine>(m =>
                                  {
                                      m.ProductId = productId;
                                      m.Quantity = quantity;
                                  })
                              };

            return Test.CreateInstance<OrderAuthorizationResponseMessage>(m => { m.SagaId = sagaId; m.Success = true; m.OrderLines = hrLines; });
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
