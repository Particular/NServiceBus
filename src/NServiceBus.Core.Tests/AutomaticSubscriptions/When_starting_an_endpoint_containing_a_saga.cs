namespace NServiceBus.Core.Tests.AutomaticSubscriptions
{
    using System;
    using System.Linq;
    using NUnit.Framework;
    using Saga;
    using Unicast.Tests.Contexts;

    [TestFixture]
    public class When_starting_an_endpoint_containing_a_saga : AutoSubscriptionContext
    {
        [Test]
        public void Should_autoSubscribe_the_saga_messageHandler_by_default()
        {
            var eventEndpointAddress = "PublisherAddress@localhost";

            RegisterMessageType<EventMessage>(eventEndpointAddress);
            RegisterMessageHandlerType<MySaga>();

            Assert.True(autoSubscriptionStrategy.GetEventsToSubscribe().Any(), "Events only handled by sagas should be auto subscribed");
        }

        [Test]
        public void Should_not_autoSubscribe_the_saga_messageHandler_on_demand()
        {
            var eventEndpointAddress = "PublisherAddress@localhost";

            RegisterMessageType<EventMessage>(eventEndpointAddress);
            RegisterMessageHandlerType<MySaga>();

            autoSubscriptionStrategy.DoNotAutoSubscribeSagas = true;

            Assert.False(autoSubscriptionStrategy.GetEventsToSubscribe().Any(), "Events only handled by sagas should not be auto subscribed on demand");
        }

        public class MySaga : Saga<MySaga.MySagaData>, IAmStartedByMessages<EventMessage>
        {
            public void Handle(EventMessage message)
            {
                throw new NotImplementedException();
            }

            public class MySagaData : ContainSagaData
            {
            }

            protected override void ConfigureHowToFindSaga(SagaPropertyMapper<MySagaData> mapper)
            {
            }
        }
    }
}