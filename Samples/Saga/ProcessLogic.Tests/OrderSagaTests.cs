using System;
using System.Collections.Generic;
using ProcessingLogic;
using ExternalOrderMessages;
using InternalOrderMessages;
using NServiceBus.Saga;
using NUnit.Framework;
using NServiceBus.Testing;

namespace ProcessLogic.Tests
{
    [TestFixture]
    public class OrderSagaTests
    {
        private OrderSaga orderSaga = null;
        private Saga Saga;
        
        [SetUp]
        public void Setup()
        {
            Saga = Saga.Test(out orderSaga);
        }

        [Test]
        public void OrderProcessingShouldCompleteAfterOneAuthorizationAndOneTimeout()
        {
            Guid externalOrderId = Guid.NewGuid();
            Guid customerId = Guid.NewGuid();
            string clientAddress = "client";

            CreateOrderMessage createOrderMsg = new CreateOrderMessage();
            createOrderMsg.OrderId = externalOrderId;
            createOrderMsg.CustomerId = customerId;
            createOrderMsg.Products = new List<Guid>(new Guid[] { Guid.NewGuid() });
            createOrderMsg.Amounts = new List<float>(new float[] { 10.0F });
            createOrderMsg.Completed = true;

            TimeoutMessage timeoutMessage = null;

            Saga.WhenReceivesMessageFrom(clientAddress)
                .ExpectSend<AuthorizeOrderRequestMessage>(
                    delegate(AuthorizeOrderRequestMessage m)
                    {
                        return m.SagaId == orderSaga.Id;
                    })
                .ExpectSend<AuthorizeOrderRequestMessage>(
                    delegate(AuthorizeOrderRequestMessage m)
                    {
                        return m.SagaId == orderSaga.Id;
                    })
                .ExpectSendToDestination<OrderStatusUpdatedMessage>(
                    delegate(string destination, OrderStatusUpdatedMessage m)
                    {
                        return m.OrderId == externalOrderId && destination == clientAddress;
                    })
                .ExpectSend<TimeoutMessage>(
                    delegate(TimeoutMessage m)
                    {
                        timeoutMessage = m;
                        return m.SagaId == orderSaga.Id;
                    })
                .When(delegate { orderSaga.Handle(createOrderMsg); });


            Assert.IsFalse(orderSaga.Completed);

            AuthorizeOrderResponseMessage response = new AuthorizeOrderResponseMessage();
            response.ManagerId = Guid.NewGuid();
            response.Authorized = true;
            response.SagaId = orderSaga.Id;

            Saga.ExpectSendToDestination<OrderStatusUpdatedMessage>(
                    delegate(string destination, OrderStatusUpdatedMessage m)
                    {
                        return (destination == clientAddress &&
                                m.OrderId == externalOrderId &&
                                m.Status == OrderStatus.Authorized1);
                    })
                .When(delegate { orderSaga.Handle(response); });

            Assert.IsFalse(orderSaga.Completed);

            Saga.ExpectSendToDestination<OrderStatusUpdatedMessage>(
                    delegate(string destination, OrderStatusUpdatedMessage m)
                    {
                        return (destination == clientAddress &&
                                m.OrderId == externalOrderId &&
                                m.Status == OrderStatus.Accepted);
                    })
                .ExpectPublish<OrderAcceptedMessage>(
                    delegate(OrderAcceptedMessage m)
                    {
                        return (m.CustomerId == customerId);
                    })
                .When(delegate { orderSaga.Timeout(timeoutMessage.State); });

            Assert.IsTrue(orderSaga.Completed);

        }


    }
}
