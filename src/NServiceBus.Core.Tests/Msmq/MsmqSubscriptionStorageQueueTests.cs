namespace NServiceBus.Core.Tests.Msmq
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Messaging;
    using System.Threading.Tasks;
    using Extensibility;
    using Unicast.Subscriptions.MessageDrivenSubscriptions;
    using NUnit.Framework;
    using MessageType = Unicast.Subscriptions.MessageType;

    [TestFixture]
    public class MsmqSubscriptionStorageQueueTests
    {
        [Test]
        public async Task Subscribe_and_unsubscribe_is_persistent()
        {
            var queue = new FakeStorageQueue();
            var storage = CreateAndInit(queue);

            var messageType = new MessageType(typeof(SomeMessage));
            var messageTypes = new[] {messageType};
            await storage.Subscribe(new Subscriber("sub1", null), messageType, new ContextBag());

            storage = CreateAndInit(queue);

            var subscribers = await storage.GetSubscriberAddressesForMessage(messageTypes, new ContextBag());
            Assert.AreEqual(1, subscribers.Count());

            await storage.Unsubscribe(new Subscriber("sub1", null), messageType, new ContextBag());

            storage = CreateAndInit(queue);
            subscribers = await storage.GetSubscriberAddressesForMessage(messageTypes, new ContextBag());
            Assert.AreEqual(0, subscribers.Count());
        }

        [Test]
        public async Task Subscribers_are_deduplicated_based_on_transport_address_comparison_case_invariant()
        {
            var queue = new FakeStorageQueue();
            var storage = CreateAndInit(queue);

            var messageType = new MessageType(typeof(SomeMessage));
            var messageTypes = new[] { messageType };
            await storage.Subscribe(new Subscriber("sub1", null), messageType, new ContextBag());
            await storage.Subscribe(new Subscriber("SUB1", null), messageType, new ContextBag());

            var subscribers = await storage.GetSubscriberAddressesForMessage(messageTypes, new ContextBag());
            Assert.AreEqual(1, subscribers.Count());
        }

        [Test]
        public async Task Can_have_multiple_subscribers_to_same_event_type()
        {
            var queue = new FakeStorageQueue();
            var storage = CreateAndInit(queue);

            var messageType = new MessageType(typeof(SomeMessage));
            var messageTypes = new[] { messageType };
            await storage.Subscribe(new Subscriber("sub1", null), messageType, new ContextBag());
            await storage.Subscribe(new Subscriber("sub2", null), messageType, new ContextBag());

            var subscribers = await storage.GetSubscriberAddressesForMessage(messageTypes, new ContextBag());
            Assert.AreEqual(2, subscribers.Count());
        }

        [Test]
        public async Task Can_handle_legacy_and_new_format()
        {
            var queue = new FakeStorageQueue();
            var storage = CreateAndInit(queue);

            var messageType = new MessageType(typeof(SomeMessage));
            var messageTypes = new[] { messageType };
            await storage.Subscribe(new Subscriber("legacy", null), messageType, new ContextBag());
            await storage.Subscribe(new Subscriber("new", "endpoint"), messageType, new ContextBag());

            var subscribers = (await storage.GetSubscriberAddressesForMessage(messageTypes, new ContextBag())).ToArray();

            Assert.AreEqual(2, subscribers.Length);
            Assert.IsTrue(subscribers.Any(s => s.TransportAddress == "legacy" && s.Endpoint == null));
            Assert.IsTrue(subscribers.Any(s => s.TransportAddress == "new" && s.Endpoint == "endpoint"));
        }

        [Test]
        public async Task Can_subscribe_to_multiple_events()
        {
            var queue = new FakeStorageQueue();
            var storage = CreateAndInit(queue);

            var someMessageType = new MessageType(typeof(SomeMessage));
            await storage.Subscribe(new Subscriber("sub1", null), someMessageType, new ContextBag());
            var otherMessageType = new MessageType(typeof(OtherMessage));
            await storage.Subscribe(new Subscriber("sub1", null), otherMessageType, new ContextBag());

            var subscribers = await storage.GetSubscriberAddressesForMessage(new[] { someMessageType }, new ContextBag());
            Assert.AreEqual(1, subscribers.Count());

            subscribers = await storage.GetSubscriberAddressesForMessage(new[] { otherMessageType }, new ContextBag());
            Assert.AreEqual(1, subscribers.Count());
        }

        [Test]
        public async Task Can_subscribe_to_multiple_events_at_once()
        {
            var queue = new FakeStorageQueue();
            var storage = CreateAndInit(queue);

            var someMessageType = new MessageType(typeof(SomeMessage));
            var otherMessageType = new MessageType(typeof(OtherMessage));
            await storage.Subscribe(new Subscriber("sub1", null), someMessageType, new ContextBag());
            await storage.Subscribe(new Subscriber("sub1", null), otherMessageType, new ContextBag());

            var subscribers = await storage.GetSubscriberAddressesForMessage(new[] { someMessageType }, new ContextBag());
            Assert.AreEqual(1, subscribers.Count());

            subscribers = await storage.GetSubscriberAddressesForMessage(new[] { otherMessageType }, new ContextBag());
            Assert.AreEqual(1, subscribers.Count());
        }

        [Test]
        public async Task Two_subscribers_with_same_address_but_different_endpoint_are_considered_duplicates()
        {
            var queue = new FakeStorageQueue();
            var storage = CreateAndInit(queue);

            var messageType = new MessageType(typeof(SomeMessage));
            var messageTypes = new[] { messageType };
            await storage.Subscribe(new Subscriber("sub1", null), messageType, new ContextBag());
            await storage.Subscribe(new Subscriber("sub1", "endpoint"), messageType, new ContextBag());

            var subscribers = await storage.GetSubscriberAddressesForMessage(messageTypes, new ContextBag());

            var subscriber = subscribers.Single();
            Assert.AreEqual(subscriber.TransportAddress, "sub1");
            Assert.AreEqual(subscriber.Endpoint, "endpoint");
        }

        [Test]
        public async Task Unsubscribing_removes_all_subscriptions_with_same_address_but_different_endpoint_names()
        {
            var queue = new FakeStorageQueue();
            var storage = CreateAndInit(queue);

            var messageType = new MessageType(typeof(SomeMessage));
            var messageTypes = new[] { messageType };
            await storage.Subscribe(new Subscriber("sub1", "e1"), messageType, new ContextBag());
            await storage.Subscribe(new Subscriber("sub1", "e2"), messageType, new ContextBag());

            await storage.Unsubscribe(new Subscriber("sub1", "e3"), messageType, new ContextBag());

            var subscribers = await storage.GetSubscriberAddressesForMessage(messageTypes, new ContextBag());
            Assert.AreEqual(0, subscribers.Count());
        }

        [Test]
        public async Task Should_handle_subscriptions_from_v5()
        {
            var queue = new FakeStorageQueue();
            var storage = CreateAndInit(queue);

            var messageType = new MessageType(typeof(SomeMessage));
            await storage.Subscribe(new Subscriber("sub1", null), messageType, new ContextBag());

            var subscriber = (await storage.GetSubscriberAddressesForMessage(new[] { messageType }, new ContextBag())).Single();

            Assert.That(subscriber.TransportAddress, Is.EqualTo("sub1"));
            Assert.That(subscriber.Endpoint, Is.Null);
        }

        [Test]
        public async Task Should_read_subscriptions_from_queue_when_initializing()
        {
            var messageType = new MessageType(typeof(SomeMessage));

            var queue = new FakeStorageQueue();
            queue.Send(new Message
            {
                Label = "subscriberA|endpointA",
                Body = $"{messageType.TypeName}, Version={messageType.Version}"
            });
            queue.Send(new Message
            {
                Label = "subscriberB|",
                Body = $"{messageType.TypeName}, Version={messageType.Version}"
            });
            var storage = CreateAndInit(queue);

            var subscribers = (await storage.GetSubscriberAddressesForMessage(new[]
            {
                messageType
            }, new ContextBag())).ToArray();

            Assert.That(subscribers.Length, Is.EqualTo(2));
            Assert.That(subscribers, Has.Exactly(1).Matches<Subscriber>(s => s.TransportAddress == "subscriberA" && s.Endpoint == "endpointA"));
            Assert.That(subscribers, Has.Exactly(1).Matches<Subscriber>(s => s.TransportAddress == "subscriberB" && s.Endpoint == null));
        }

        [Test]
        public async Task Should_deduplicate_subscribers()
        {
            var queue = new FakeStorageQueue();
            var storage = CreateAndInit(queue);

            var messageType1 = new MessageType(typeof(SomeMessage));
            var messageType2 = new MessageType(typeof(OtherMessage));
            var messageType3 = new MessageType(typeof(AnotherMessage));
            await storage.Subscribe(new Subscriber("sub1", "endpointA"), messageType1, new ContextBag());
            await storage.Subscribe(new Subscriber("sub1", "endpointA"), messageType2, new ContextBag());
            await storage.Subscribe(new Subscriber("sub1", null), messageType3, new ContextBag());

            var subscribers = (await storage.GetSubscriberAddressesForMessage(new[]
            {
                messageType1, messageType2, messageType3
            }, new ContextBag())).ToArray();

            Assert.That(subscribers.Length, Is.EqualTo(2));
            Assert.That(subscribers, Has.Exactly(1).Matches<Subscriber>(s => s.TransportAddress == "sub1" && s.Endpoint == null));
            Assert.That(subscribers, Has.Exactly(1).Matches<Subscriber>(s => s.TransportAddress == "sub1" && s.Endpoint == "endpointA"));
        }

        static MsmqSubscriptionStorage CreateAndInit(FakeStorageQueue queue)
        {
            var storage = new MsmqSubscriptionStorage(queue);
            storage.Init();
            return storage;
        }

        class SomeMessage
        {
        }

        class OtherMessage
        {
        }

        class AnotherMessage
        {
        }

        class FakeStorageQueue : IMsmqSubscriptionStorageQueue
        {
            readonly List<Message> q = new List<Message>();

            public IEnumerable<Message> GetAllMessages()
            {
                return q.ToArray();
            }

            public void Send(Message toSend)
            {
                q.Add(toSend);
            }

            public void ReceiveById(string messageId)
            {
                q.RemoveAll(m => m.Id == messageId);
            }
        }
    }
}