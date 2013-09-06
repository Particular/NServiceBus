namespace NServiceBus.Transports.RabbitMQ.Tests
{
    using System;
    using NUnit.Framework;

    [TestFixture]
    [Explicit("requires rabbit node")]
    public class When_subscribed_to_a_event : RabbitMqContext
    {

        [Test]
        public void Should_receive_published_events_of_that_type()
        {
            Subscribe<MyEvent>();

            Publish<MyEvent>();

            AssertReceived<MyEvent>();
        }


        [Test]
        public void Should_receive_the_event_if_subscribed_to_the_base_class()
        {
            Subscribe<EventBase>();

            Publish<SubEvent1>();
            Publish<SubEvent2>();

            AssertReceived<SubEvent1>();
            AssertReceived<SubEvent2>();
        }


        [Test]
        public void Should_not_receive_the_event_if_subscribed_to_the_specific_class()
        {
            Subscribe<SubEvent1>();

            Publish<EventBase>();
            AssertNoEventReceived();
        }



        [Test]
        public void Should_receive_the_event_if_subscribed_to_the_base_interface()
        {
            Subscribe<IMyEvent>();

            Publish<MyEvent1>();
            Publish<MyEvent2>();

            AssertReceived<MyEvent1>();
            AssertReceived<MyEvent2>();
        }

        [Test]
        public void Should_not_receive_the_event_if_subscribed_to_specific_interface()
        {
            Subscribe<MyEvent1>();

            Publish<IMyEvent>();
            AssertNoEventReceived();
        }


        [Test]
        public void Should_not_receive_events_of_other_types()
        {
            Subscribe<MyEvent>();

            //publish a event that that this publisher isn't subscribed to
            Publish<MyOtherEvent>();
            Publish<MyEvent>();

            AssertReceived<MyEvent>();
        }

        [Test]
        public void Subscribing_to_IEvent_should_subscribe_to_all_published_messages()
        {
            Subscribe<IEvent>();

            Publish<MyOtherEvent>();
            Publish<MyEvent>();

            AssertReceived<MyOtherEvent>();
            AssertReceived<MyEvent>();
        }

        [Test]
        public void Subscribing_to_Object_should_subscribe_to_all_published_messages()
        {
            Subscribe<object>();

            Publish<MyOtherEvent>();
            Publish<MyEvent>();

            AssertReceived<MyOtherEvent>();
            AssertReceived<MyEvent>();
        }

        [Test]
        public void Subscribing_to_a_class_implementing_a_interface_should_only_give_the_concrete_class()
        {
            Subscribe<CombinedClassAndInterface>();

            Publish<CombinedClassAndInterface>();
            Publish<IMyEvent>();

            AssertReceived<CombinedClassAndInterface>();
            AssertNoEventReceived();
        }


        [Test]
        public void Subscribing_to_a_interface_that_is_implemented_be_a_class_should_give_the_event_if_the_class_is_published()
        {
            Subscribe<IMyEvent>();

            Publish<CombinedClassAndInterface>();
            Publish<IMyEvent>();

            AssertReceived<CombinedClassAndInterface>();
            AssertReceived<IMyEvent>();
        }


        [Test]
        public void Should_not_receive_events_after_unsubscribing()
        {
            Subscribe<MyEvent>();

            subscriptionManager.Unsubscribe(typeof(MyEvent), Address.Parse(ExchangeNameConvention(null, null)));

            //publish a event that that this publisher isn't subscribed to
            Publish<MyEvent>();

            AssertNoEventReceived();
        }

        void Subscribe<T>()
        {
            subscriptionManager.Subscribe(typeof(T), Address.Parse(ExchangeNameConvention(null,null)));
        }

        void Publish<T>()
        {
            var type = typeof(T);
            MessagePublisher.Publish(new TransportMessage { CorrelationId = type.FullName }, new[] { type });

        }


        void AssertReceived<T>()
        {
            var receivedEvent = WaitForMessage();

            AssertReceived<T>(receivedEvent);
        }

        void AssertReceived<T>(TransportMessage receivedEvent)
        {
            Assert.AreEqual(typeof(T).FullName, receivedEvent.CorrelationId);

        }

        void AssertNoEventReceived()
        {
            var receivedEvent = WaitForMessage();

            Assert.Null(receivedEvent);
        }

        protected override string ExchangeNameConvention(Address address,Type eventType)
        {
            return "nservicebus.events";
        }

    }


    public class MyOtherEvent
    {
    }

    public class MyEvent : IMessage
    {
    }

    public class EventBase : IEvent
    {

    }

    public class SubEvent1 : EventBase
    {

    }

    public class SubEvent2 : EventBase
    {

    }


    public interface IMyEvent : IEvent
    {

    }

    public interface MyEvent1 : IMyEvent
    {

    }

    public interface MyEvent2 : IMyEvent
    {

    }


    public class CombinedClassAndInterface : IMyEvent
    {

    }


}