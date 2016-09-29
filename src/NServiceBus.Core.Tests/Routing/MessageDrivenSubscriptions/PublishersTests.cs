namespace NServiceBus.Core.Tests.Routing.MessageDrivenSubscriptions
{
    using System.Collections.Generic;
    using System.Linq;
    using NServiceBus.Routing;
    using NServiceBus.Routing.MessageDrivenSubscriptions;
    using NUnit.Framework;

    [TestFixture]
    public class PublishersTests
    {
        [Test]
        public void When_group_does_not_exist_should_add_routes()
        {
            var publisherTable = new Publishers();
            var publisher = PublisherAddress.CreateFromEndpointName("Endpoint1");
            publisherTable.AddOrReplacePublishers("key", new List<PublisherTableEntry>
            {
                new PublisherTableEntry(typeof(MyEvent), publisher),
            });

            var retrievedPublisher = publisherTable.GetPublisherFor(typeof(MyEvent)).Single();
            Assert.AreSame(publisher, retrievedPublisher);
        }

        [Test]
        public void When_group_exists_should_replace_existing_routes()
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

            var retrievedPublisher = publisherTable.GetPublisherFor(typeof(MyEvent)).Single();
            Assert.AreSame(newPublisher, retrievedPublisher);
        }

        [Test]
        public void When_multiple_publishers_exist_should_return_all_of_them()
        {
            var publisherTable = new Publishers();

            var pub1 = PublisherAddress.CreateFromEndpointName("Endpoint1");
            var pub2 = PublisherAddress.CreateFromEndpointName("Endpoint2");

            publisherTable.AddOrReplacePublishers("key2", new List<PublisherTableEntry>
            {
                new PublisherTableEntry(typeof(MyEvent), pub1),
            });
            publisherTable.AddOrReplacePublishers("key1", new List<PublisherTableEntry>
            {
                new PublisherTableEntry(typeof(MyEvent), pub2),
            });

            var pubs = publisherTable.GetPublisherFor(typeof(MyEvent)).ToArray();

            Assert.Contains(pub1, pubs);
            Assert.Contains(pub2, pubs);
        }

        [Test]
        public void When_same_publisher_is_registered_multiple_times_should_remove_duplicates()
        {
            var publisherTable = new Publishers();

            var pub1 = PublisherAddress.CreateFromEndpointName("Endpoint1");
            var pub2 = PublisherAddress.CreateFromEndpointName("Endpoint1");
            var pub3 = PublisherAddress.CreateFromEndpointInstances(new EndpointInstance("Instance1"), new EndpointInstance("Instance2"));
            var pub4 = PublisherAddress.CreateFromEndpointInstances(new EndpointInstance("Instance1"), new EndpointInstance("Instance2"));
            var pub5 = PublisherAddress.CreateFromPhysicalAddresses("address1", "address2");
            var pub6 = PublisherAddress.CreateFromPhysicalAddresses("address1", "address2");

            publisherTable.AddOrReplacePublishers("key2", new List<PublisherTableEntry>
            {
                new PublisherTableEntry(typeof(MyEvent), pub1),
                new PublisherTableEntry(typeof(MyEvent), pub2),
                new PublisherTableEntry(typeof(MyEvent), pub3),
                new PublisherTableEntry(typeof(MyEvent), pub4),
                new PublisherTableEntry(typeof(MyEvent), pub5),
                new PublisherTableEntry(typeof(MyEvent), pub6)
            });

            var pubs = publisherTable.GetPublisherFor(typeof(MyEvent)).ToArray();

            Assert.AreEqual(3, pubs.Length);
            Assert.Contains(pub1, pubs);
            Assert.Contains(pub2, pubs);
            Assert.Contains(pub3, pubs);
            Assert.Contains(pub4, pubs);
            Assert.Contains(pub5, pubs);
            Assert.Contains(pub6, pubs);
        }

        class MyEvent : IEvent
        {
        }

        class MyEvent2 : IEvent
        {
        }

        class MyEvent3 : IEvent
        {
        }
    }
}
