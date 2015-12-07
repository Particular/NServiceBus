namespace NServiceBus.Core.Tests.Msmq
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Messaging;
    using System.Threading.Tasks;
    using NServiceBus.Extensibility;
    using NServiceBus.Unicast.Subscriptions.MessageDrivenSubscriptions;
    using NUnit.Framework;
    using MessageType = NServiceBus.Unicast.Subscriptions.MessageType;

    [TestFixture]
    public class MsmqSubscriptionStorageQueueTests
    {
        [Test]
        public async Task Subscribe_and_unsubscribe_is_persistent()
        {
            var queue = new FakeStorageQueue();
            var storage = CreateAndInit(queue);

            var messageTypes = new[] {new MessageType(typeof(SomeMessage))};
            await storage.Subscribe(new Subscriber("sub1", null), messageTypes, new ContextBag());

            storage = CreateAndInit(queue);

            var subscribers = await storage.GetSubscriberAddressesForMessage(messageTypes, new ContextBag());
            Assert.AreEqual(1, subscribers.Count());

            await storage.Unsubscribe(new Subscriber("sub1", null), messageTypes, new ContextBag());

            storage = CreateAndInit(queue);
            subscribers = await storage.GetSubscriberAddressesForMessage(messageTypes, new ContextBag());
            Assert.AreEqual(0, subscribers.Count());
        }

        static MsmqSubscriptionStorage CreateAndInit(FakeStorageQueue queue)
        {
            var storage = new MsmqSubscriptionStorage(queue);
            storage.Init();
            return storage;
        }

        [Test]
        public async Task Subscribers_are_deduplicated_based_on_transport_address_comparison_case_invariant()
        {
            var queue = new FakeStorageQueue();
            var storage = CreateAndInit(queue);

            var messageTypes = new[] { new MessageType(typeof(SomeMessage)) };
            await storage.Subscribe(new Subscriber("sub1", null), messageTypes, new ContextBag());
            await storage.Subscribe(new Subscriber("SUB1", null), messageTypes, new ContextBag());

            var subscribers = await storage.GetSubscriberAddressesForMessage(messageTypes, new ContextBag());
            Assert.AreEqual(1, subscribers.Count());
        }

        [Test]
        public async Task Can_have_multiple_subscribers_to_same_event_type()
        {
            var queue = new FakeStorageQueue();
            var storage = CreateAndInit(queue);

            var messageTypes = new[] { new MessageType(typeof(SomeMessage)) };
            await storage.Subscribe(new Subscriber("sub1", null), messageTypes, new ContextBag());
            await storage.Subscribe(new Subscriber("sub2", null), messageTypes, new ContextBag());

            var subscribers = await storage.GetSubscriberAddressesForMessage(messageTypes, new ContextBag());
            Assert.AreEqual(2, subscribers.Count());
        }

        [Test]
        public async Task Can_handle_legacy_and_new_format()
        {
            var queue = new FakeStorageQueue();
            var storage = CreateAndInit(queue);

            var messageTypes = new[] { new MessageType(typeof(SomeMessage)) };
            await storage.Subscribe(new Subscriber("legacy", null), messageTypes, new ContextBag());
            await storage.Subscribe(new Subscriber("new", new Endpoint("endpoint")), messageTypes, new ContextBag());

            var subscribers = (await storage.GetSubscriberAddressesForMessage(messageTypes, new ContextBag())).ToArray();

            Assert.AreEqual(2, subscribers.Length);
            Assert.IsTrue(subscribers.Any(s => s.TransportAddress == "legacy" && s.Endpoint == null));
            Assert.IsTrue(subscribers.Any(s => s.TransportAddress == "new" && s.Endpoint == new Endpoint("endpoint")));
        }


        [Test]
        public async Task Can_subscribe_to_multiple_events()
        {
            var queue = new FakeStorageQueue();
            var storage = CreateAndInit(queue);

            await storage.Subscribe(new Subscriber("sub1", null), new[] { new MessageType(typeof(SomeMessage)) }, new ContextBag());
            await storage.Subscribe(new Subscriber("sub1", null), new[] { new MessageType(typeof(OtherMessage)) }, new ContextBag());

            var subscribers = await storage.GetSubscriberAddressesForMessage(new[] { new MessageType(typeof(SomeMessage)) }, new ContextBag());
            Assert.AreEqual(1, subscribers.Count());

            subscribers = await storage.GetSubscriberAddressesForMessage(new[] { new MessageType(typeof(OtherMessage)) }, new ContextBag());
            Assert.AreEqual(1, subscribers.Count());
        }

        [Test]
        public async Task Can_subscribe_to_multiple_events_at_once()
        {
            var queue = new FakeStorageQueue();
            var storage = CreateAndInit(queue);

            await storage.Subscribe(new Subscriber("sub1", null), new[] { new MessageType(typeof(SomeMessage)), new MessageType(typeof(OtherMessage)) }, new ContextBag());

            var subscribers = await storage.GetSubscriberAddressesForMessage(new[] { new MessageType(typeof(SomeMessage)) }, new ContextBag());
            Assert.AreEqual(1, subscribers.Count());

            subscribers = await storage.GetSubscriberAddressesForMessage(new[] { new MessageType(typeof(OtherMessage)) }, new ContextBag());
            Assert.AreEqual(1, subscribers.Count());
        }

        [Test]
        public async Task Two_subscribers_with_same_address_but_different_endpoint_are_considered_duplicates()
        {
            var queue = new FakeStorageQueue();
            var storage = CreateAndInit(queue);

            var messageTypes = new[] { new MessageType(typeof(SomeMessage)) };
            await storage.Subscribe(new Subscriber("sub1", null), messageTypes, new ContextBag());
            await storage.Subscribe(new Subscriber("sub1", new Endpoint("endpoint")), messageTypes, new ContextBag());

            var subscribers = await storage.GetSubscriberAddressesForMessage(messageTypes, new ContextBag());
            Assert.AreEqual(1, subscribers.Count());
        }

        [Test]
        public async Task Unsubscribing_removes_all_subscriptions_with_same_address_but_different_endpoint_names()
        {
            var queue = new FakeStorageQueue();
            var storage = CreateAndInit(queue);

            var messageTypes = new[] { new MessageType(typeof(SomeMessage)) };
            await storage.Subscribe(new Subscriber("sub1", new Endpoint("e1")), messageTypes, new ContextBag());
            await storage.Subscribe(new Subscriber("sub1", new Endpoint("e2")), messageTypes, new ContextBag());

            await storage.Unsubscribe(new Subscriber("sub1", new Endpoint("e3")), messageTypes, new ContextBag());

            var subscribers = await storage.GetSubscriberAddressesForMessage(messageTypes, new ContextBag());
            Assert.AreEqual(0, subscribers.Count());
        }

        class SomeMessage : IMessage
        {
        }

        class OtherMessage : IMessage
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