namespace NServiceBus.Core.Tests.Routing.MessageDrivenSubscriptions
{
    using System.Collections.Generic;
    using NServiceBus.Routing.MessageDrivenSubscriptions;
    using NUnit.Framework;

    [TestFixture]
    public class PublishersTests
    {
        [Test]
        public void When_group_does_not_exist_routes_are_added()
        {
            var publisherTable = new Publishers();
            var publisher = PublisherAddress.CreateFromEndpointName("Endpoint1");
            publisherTable.AddOrReplacePublishers("key", new List<PublisherTableEntry>
            {
                new PublisherTableEntry(typeof(MyEvent), publisher),
            });

            var retrievedPublisher = publisherTable.GetPublisherFor(typeof(MyEvent));
            Assert.AreSame(publisher, retrievedPublisher);
        }

        [Test]
        public void When_group_exists_routes_are_replaced()
        {
            var publisherTable = new Publishers();
            var oldPublisher = PublisherAddress.CreateFromEndpointName("Endpoint1");
            var newPublisher = PublisherAddress.CreateFromEndpointName("Endpoint2");
            publisherTable.AddOrReplacePublishers("key", new List<PublisherTableEntry>
            {
                new PublisherTableEntry(typeof(MyEvent), oldPublisher),
            });

            publisherTable.AddOrReplacePublishers("key", new List<PublisherTableEntry>
            {
                new PublisherTableEntry(typeof(MyEvent), newPublisher),
            });

            var retrievedPublisher = publisherTable.GetPublisherFor(typeof(MyEvent));
            Assert.AreSame(newPublisher, retrievedPublisher);
        }

        [Test]
        public void When_routes_are_ambiguous_it_throws_exception()
        {
            var publisherTable = new Publishers();
            var lowPriorityPublisher = PublisherAddress.CreateFromEndpointName("Endpoint1");
            var highPriorityPublisher = PublisherAddress.CreateFromEndpointName("Endpoint1");

            publisherTable.AddOrReplacePublishers("key2", new List<PublisherTableEntry>
            {
                new PublisherTableEntry(typeof(MyEvent), highPriorityPublisher),
            });

            Assert.That(() =>
            {
                publisherTable.AddOrReplacePublishers("key1", new List<PublisherTableEntry>
                {
                    new PublisherTableEntry(typeof(MyEvent), lowPriorityPublisher),
                });
            }, Throws.Exception);
        }

        class MyEvent : IEvent
        {
        }
    }
}
