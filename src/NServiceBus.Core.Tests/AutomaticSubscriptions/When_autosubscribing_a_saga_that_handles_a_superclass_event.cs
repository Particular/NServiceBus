namespace NServiceBus.Core.Tests.AutomaticSubscriptions
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using NUnit.Framework;
    using Saga;

    [TestFixture]
    public class When_autoSubscribing_a_saga_that_handles_a_superclass_event : AutoSubscriptionContext
    {
        protected override IEnumerable<Type> KnownMessageTypes()
        {
            return new[] { typeof(EventWithParent), typeof(EventMessageBase) };
        }
        [Test]
        public void Should_autoSubscribe_the_saga_messageHandler()
        {

            var eventEndpointAddress = "PublisherAddress@localhost";

            RegisterMessageType<EventWithParent>(eventEndpointAddress);
            RegisterMessageHandlerType<MySagaThatReactsOnASuperClassEvent>();

            Assert.True(autoSubscriptionStrategy.GetEventsToSubscribe().Any(), "Saga event handler should be subscribed");
        }

        public class MySagaThatReactsOnASuperClassEvent : Saga<MySagaThatReactsOnASuperClassEvent.MySagaData>, IAmStartedByMessages<EventMessageBase>
        {
            public void Handle(EventMessageBase message)
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
        public class EventMessageBase : IEvent
        {

        }


        public class EventWithParent : EventMessageBase
        {

        }

    }
}