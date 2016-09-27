namespace NServiceBus.Core.Tests.Msmq
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Extensibility;
    using Unicast.Subscriptions.MessageDrivenSubscriptions;
    using NUnit.Framework;
    using MessageType = Unicast.Subscriptions.MessageType;

    [TestFixture]
    public class MsmqSubscriptionStorageQueueTests
    {
        [Test]
        public async Task Subscribe_is_persistent()
        {
            var queue = new FakeStorageQueue();
            var messageType = new MessageType(typeof(SomeMessage));
            var storage = CreateAndInit(queue);

            await storage.Subscribe(new Subscriber("sub1", null), messageType, new ContextBag());
            await storage.Subscribe(new Subscriber("sub2", "endpointA"), messageType, new ContextBag());

            var storedMessages = queue.GetAllMessages().ToArray();
            Assert.That(storedMessages.Length, Is.EqualTo(2));

            storage = CreateAndInit(queue);
            var subscribers = (await storage.GetSubscriberAddressesForMessage(new[] { messageType }, new ContextBag())).ToArray();
            Assert.That(subscribers, Has.Exactly(1).Matches<Subscriber>(s => s.TransportAddress == "sub1" && s.Endpoint == null));
            Assert.That(subscribers, Has.Exactly(1).Matches<Subscriber>(s => s.TransportAddress == "sub2" && s.Endpoint == "endpointA"));
        }

        [Test]
        public async Task Unsubscribe_is_persistent()
        {
            var queue = new FakeStorageQueue();
            var storage = CreateAndInit(queue);

            var messageType = new MessageType(typeof(SomeMessage));
            await storage.Subscribe(new Subscriber("sub1", null), messageType, new ContextBag());
            storage = CreateAndInit(queue);

            await storage.Unsubscribe(new Subscriber("sub1", "endpointA"), messageType, new ContextBag());
            Assert.That(queue.GetAllMessages(), Is.Empty);

            storage = CreateAndInit(queue);
            var subscribers = await storage.GetSubscriberAddressesForMessage(new[] { messageType }, new ContextBag());
            Assert.AreEqual(0, subscribers.Count());
        }

        [Test]
        public async Task Remove_outdated_subscriptions_on_initialization()
        {
            var queue = new FakeStorageQueue();
            var storage = CreateAndInit(queue);

            var messageType = new MessageType(typeof(SomeMessage));
            await storage.Subscribe(new Subscriber("sub1", "1"), messageType, new ContextBag());
            await storage.Subscribe(new Subscriber("sub1", "2"), messageType, new ContextBag());
            await storage.Subscribe(new Subscriber("sub1", "3"), messageType, new ContextBag());

            storage = CreateAndInit(queue);
            var subscribers = (await storage.GetSubscriberAddressesForMessage(new[] { messageType }, new ContextBag())).ToArray();

            Assert.That(subscribers.Length, Is.EqualTo(1));
            Assert.That(subscribers[0].TransportAddress, Is.EqualTo("sub1"));
            Assert.That(subscribers[0].Endpoint, Is.EqualTo("3"));
            Assert.That(queue.GetAllMessages().Count(), Is.EqualTo(1));
        }

        [Test]
        public async Task Subscribers_are_deduplicated_based_on_transport_address_comparison_case_invariant()
        {
            var queue = new FakeStorageQueue();
            var storage = CreateAndInit(queue);

            var messageType = new MessageType(typeof(SomeMessage));
            await storage.Subscribe(new Subscriber("sub1", null), messageType, new ContextBag());
            await storage.Subscribe(new Subscriber("SUB1", null), messageType, new ContextBag());

            var subscribers = await storage.GetSubscriberAddressesForMessage(new[] { messageType }, new ContextBag());
            Assert.AreEqual(1, subscribers.Count());
        }

        [Test]
        public async Task Can_have_multiple_subscribers_to_same_event_type()
        {
            var queue = new FakeStorageQueue();
            var storage = CreateAndInit(queue);

            var messageType = new MessageType(typeof(SomeMessage));
            await storage.Subscribe(new Subscriber("sub1", null), messageType, new ContextBag());
            await storage.Subscribe(new Subscriber("sub2", null), messageType, new ContextBag());

            var subscribers = await storage.GetSubscriberAddressesForMessage(new[] { messageType }, new ContextBag());
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
            var otherMessageType = new MessageType(typeof(OtherMessage));
            await storage.Subscribe(new Subscriber("sub1", null), someMessageType, new ContextBag());
            await storage.Subscribe(new Subscriber("sub1", null), otherMessageType, new ContextBag());

            var subscribers = await storage.GetSubscriberAddressesForMessage(new[] { someMessageType }, new ContextBag());
            Assert.AreEqual(1, subscribers.Count());

            subscribers = await storage.GetSubscriberAddressesForMessage(new[] { otherMessageType }, new ContextBag());
            Assert.AreEqual(1, subscribers.Count());
        }

        [Test]
        public async Task Same_subscriber_for_multiple_message_types_is_returned_only_once()
        {
            var queue = new FakeStorageQueue();
            var storage = CreateAndInit(queue);

            var someMessageType = new MessageType(typeof(SomeMessage));
            var otherMessageType = new MessageType(typeof(OtherMessage));
            await storage.Subscribe(new Subscriber("sub1", null), someMessageType, new ContextBag());
            await storage.Subscribe(new Subscriber("sub1", null), otherMessageType, new ContextBag());

            var subscribers = await storage.GetSubscriberAddressesForMessage(new[]
            {
                someMessageType,
                otherMessageType
            }, new ContextBag());

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
            Assert.AreEqual("sub1", subscriber.TransportAddress);
            Assert.AreEqual("endpoint", subscriber.Endpoint);
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
        public void Messages_with_the_same_timestamp_have_repeatedly_same_order()
        {
            var now = DateTime.Now;

            var msg1 = new MsmqSubscriptionMessage
            {
                ArrivedTime = now,
                Id = Guid.NewGuid().ToString(),
                Body = "SomeMessageType, Version=1.0.0",
                Label = "address|endpoint"
            };
            var msg2 = new MsmqSubscriptionMessage
            {
                ArrivedTime = now,
                Id = Guid.NewGuid().ToString(),
                Body = "SomeMessageType, Version=1.0.0",
                Label = "address|endpoint"
            };

            var queue1 = new FakeStorageQueue();
            var storage1 = new MsmqSubscriptionStorage(queue1);
            queue1.Messages.AddRange(new []
            {
                msg1,
                msg2,
            });

            var queue2 = new FakeStorageQueue();
            var storage2 = new MsmqSubscriptionStorage(queue2);
            queue2.Messages.AddRange(new[]
            {
                msg2, // inverted order
                msg1,
            });

            storage1.Init();
            storage2.Init();

            // both endpoints should delete the same message although they have the same timestamp and are read in different order from the queue.
            Assert.That(queue1.Messages.Count, Is.EqualTo(1));
            Assert.AreEqual(queue1.Messages.Single(), queue2.Messages.Single());
        }

        static MsmqSubscriptionStorage CreateAndInit(FakeStorageQueue queue)
        {
            var storage = new MsmqSubscriptionStorage(queue);
            storage.Init();
            return storage;
        }

        class SomeMessage : IMessage
        {
        }

        class OtherMessage : IMessage
        {
        }

        class FakeStorageQueue : IMsmqSubscriptionStorageQueue
        {
            public readonly List<MsmqSubscriptionMessage> Messages = new List<MsmqSubscriptionMessage>();

            DateTime arrivedTime = DateTime.Now;

            public IEnumerable<MsmqSubscriptionMessage> GetAllMessages()
            {
                return Messages.ToArray();
            }

            public string Send(string body, string label)
            {
                var id = Guid.NewGuid().ToString();

                Messages.Add(new MsmqSubscriptionMessage
                {
                    ArrivedTime = arrivedTime = arrivedTime.AddMilliseconds(1),
                    Body = body,
                    Label = label,
                    Id = id
                });

                return id;
            }

            public void TryReceiveById(string messageId)
            {
                Messages.RemoveAll(m => m.Id == messageId);
            }
        }
    }
}