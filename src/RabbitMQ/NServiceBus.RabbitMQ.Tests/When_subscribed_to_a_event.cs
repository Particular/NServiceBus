namespace NServiceBus.RabbitMQ.Tests
{
    using NUnit.Framework;

    [TestFixture, Explicit("Integration tests")]
    public class When_subscribed_to_a_event : RabbitMqContext
    {
    
        [SetUp]
        public void SetUp()
        {

            subscriptionManager.Subscribe(typeof(MyEvent), Address.Parse(PUBLISHERNAME));
        }

        [Test]
        public void Should_receive_published_events_of_that_type()
        {
            MessagePublisher.Publish(new TransportMessage { CorrelationId = "myevent" }, new[] { typeof(MyEvent) });

            var receivedEvent = WaitForMessage();

            Assert.AreEqual("myevent", receivedEvent.CorrelationId);
        }

        [Test]
        public void Should_not_receive_events_of_other_types()
        {
            //publish a event that that this publisher isn't subscribed to
            MessagePublisher.Publish(new TransportMessage(), new[] { typeof(MyOtherEvent) });
            MessagePublisher.Publish(new TransportMessage { CorrelationId = "myevent" }, new[] { typeof(MyEvent) });

            var receivedEvent = WaitForMessage();

            Assert.AreEqual("myevent", receivedEvent.CorrelationId);
        }

        [Test]
        public void Should_not_receive_events_after_unsubscribing()
        {
            subscriptionManager.Unsubscribe(typeof(MyEvent), Address.Parse(PUBLISHERNAME));
            
            //publish a event that that this publisher isn't subscribed to
            MessagePublisher.Publish(new TransportMessage { CorrelationId = "myevent" }, new[] { typeof(MyEvent) });

            var receivedEvent = WaitForMessage();

            Assert.Null(receivedEvent);
        }


        public class MyOtherEvent
        {
        }

        public class MyEvent
        {
        }
    }

}