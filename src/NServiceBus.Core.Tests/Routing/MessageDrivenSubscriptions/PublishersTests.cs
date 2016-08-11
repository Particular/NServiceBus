
namespace NServiceBus.Core.Tests.Routing.MessageDrivenSubscriptions
{
    using System.Collections.Generic;
    using ApprovalTests;
    using NServiceBus.Routing;
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

        [Test]
        public void Should_log_changes()
        {
            var publishersTable = new Publishers();
            var log = "";
            publishersTable.SetLogChangeAction(x => { log = x; });
            publishersTable.AddOrReplacePublishers("key", new List<PublisherTableEntry>
            {
                new PublisherTableEntry(typeof(MyEvent), PublisherAddress.CreateFromEndpointName("Endpoint")),
                new PublisherTableEntry(typeof(MyEvent2), PublisherAddress.CreateFromEndpointInstances(new EndpointInstance("Endpoint", "X"), new EndpointInstance("Endpoint", "Y"))),
                new PublisherTableEntry(typeof(MyEvent3), PublisherAddress.CreateFromPhysicalAddresses("queue@machine1", "queue@machine2"))
            });
            Approvals.Verify(log);
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
