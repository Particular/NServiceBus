namespace NServiceBus.Core.Tests.Routing
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using NServiceBus.Routing;
    using NServiceBus.Routing.MessageDrivenSubscriptions;
    using NUnit.Framework;
    using Testing;
    using Transport;
    using Unicast.Queuing;

    [TestFixture]
    public class MessageDrivenSubscribeTerminatorTests
    {
        [SetUp]
        public void SetUp()
        {
            publishers = new Publishers();
            publishers.AddOrReplacePublishers("A", new List<PublisherTableEntry> { new PublisherTableEntry(typeof(object), PublisherAddress.CreateFromPhysicalAddresses("publisher1")) });
            router = new SubscriptionRouter(publishers, new EndpointInstances(), i => i.ToString());
            dispatcher = new FakeDispatcher();
            subscribeTerminator = new MessageDrivenSubscribeTerminator(router, new ReceiveAddresses("replyToAddress", null, null), "Endpoint", dispatcher);
        }

        [Test]
        public async Task Should_include_TimeSent_and_Version_headers()
        {
            await subscribeTerminator.Invoke(new TestableSubscribeContext(), c => Task.CompletedTask);

            foreach (var dispatchedTransportOperation in dispatcher.DispatchedTransportOperations)
            {
                var unicastTransportOperations = dispatchedTransportOperation.UnicastTransportOperations;
                var operations = new List<UnicastTransportOperation>(unicastTransportOperations);

                Assert.IsTrue(operations[0].Message.Headers.ContainsKey(Headers.TimeSent));
                Assert.IsTrue(operations[0].Message.Headers.ContainsKey(Headers.NServiceBusVersion));
            }
        }

        [Test]
        public async Task Should_Dispatch_for_all_publishers()
        {
            publishers.AddOrReplacePublishers("B", new List<PublisherTableEntry>()
            {
                new PublisherTableEntry(typeof(object), PublisherAddress.CreateFromPhysicalAddresses("publisher2"))
            });

            await subscribeTerminator.Invoke(new TestableSubscribeContext(), c => Task.CompletedTask);

            Assert.AreEqual(2, dispatcher.DispatchedTransportOperations.Count);
        }

        [Test]
        public async Task Should_Dispatch_according_to_max_retries_when_dispatch_fails()
        {
            var context = new TestableSubscribeContext();
            var state = context.Extensions.GetOrCreate<MessageDrivenSubscribeTerminator.Settings>();
            state.MaxRetries = 10;
            state.RetryDelay = TimeSpan.Zero;
            dispatcher.FailDispatch(10);

            await subscribeTerminator.Invoke(context, c => Task.CompletedTask);

            Assert.AreEqual(1, dispatcher.DispatchedTransportOperations.Count);
            Assert.AreEqual(10, dispatcher.FailedNumberOfTimes);
        }

        [Test]
        public void Should_Throw_when_max_retries_reached()
        {
            var context = new TestableSubscribeContext();
            var state = context.Extensions.GetOrCreate<MessageDrivenSubscribeTerminator.Settings>();
            state.MaxRetries = 10;
            state.RetryDelay = TimeSpan.Zero;
            dispatcher.FailDispatch(11);

            Assert.That(async () =>
            {
                await subscribeTerminator.Invoke(context, c => Task.CompletedTask);
            }, Throws.InstanceOf<QueueNotFoundException>());

            Assert.AreEqual(0, dispatcher.DispatchedTransportOperations.Count);
            Assert.AreEqual(11, dispatcher.FailedNumberOfTimes);
        }

        [Test]
        public void Should_throw_when_no_publisher_for_message_found()
        {
            // clear publishers list
            publishers.AddOrReplacePublishers("A", new List<PublisherTableEntry>());

            var exception = Assert.ThrowsAsync<Exception>(() =>
                subscribeTerminator.Invoke(new TestableSubscribeContext(), c => Task.CompletedTask));

            StringAssert.Contains($"No publisher address could be found for message type '{typeof(object)}'.", exception.Message);
        }

        [Test]
        public async Task Should_dispatch_to_all_publishers_for_all_events()
        {
            var context = new TestableSubscribeContext()
            {
                EventTypes = new[] { typeof(EventA), typeof(EventB) }
            };

            publishers.AddOrReplacePublishers("Test", new List<PublisherTableEntry>()
            {
                new PublisherTableEntry(typeof(EventA), PublisherAddress.CreateFromPhysicalAddresses("publisher1")),
                new PublisherTableEntry(typeof(EventA), PublisherAddress.CreateFromPhysicalAddresses("publisher2")),
                new PublisherTableEntry(typeof(EventB), PublisherAddress.CreateFromPhysicalAddresses("publisher1")),
                new PublisherTableEntry(typeof(EventB), PublisherAddress.CreateFromPhysicalAddresses("publisher2"))
            });

            await subscribeTerminator.Invoke(context, c => Task.CompletedTask);

            Assert.AreEqual(4, dispatcher.DispatchedTransportOperations.Count);
        }

        [Test]
        public void When_subscribing_multiple_events_should_throw_aggregate_exception_with_all_failures()
        {
            var context = new TestableSubscribeContext
            {
                EventTypes = new[] { typeof(EventA), typeof(EventB) }
            };
            // Marks this message as a SubscribeAll call
            context.Extensions.Set(MessageSession.SubscribeAllFlagKey, true);
            var state = context.Extensions.GetOrCreate<MessageDrivenSubscribeTerminator.Settings>();
            state.MaxRetries = 0;
            state.RetryDelay = TimeSpan.Zero;
            dispatcher.FailDispatch(10);

            // no publisher for EventB
            publishers.AddOrReplacePublishers("Test", new List<PublisherTableEntry>()
            {
                new PublisherTableEntry(typeof(EventA), PublisherAddress.CreateFromPhysicalAddresses("publisher1")),
            });

            var exception = Assert.ThrowsAsync<AggregateException>(() => subscribeTerminator.Invoke(context, c => Task.CompletedTask));

            Assert.AreEqual(2, exception.InnerExceptions.Count);
            Assert.IsTrue(exception.InnerExceptions.Any(e => e is QueueNotFoundException)); // exception from dispatcher
            Assert.IsTrue(exception.InnerExceptions.Any(e => e.Message.Contains($"No publisher address could be found for message type '{typeof(EventB)}'"))); // exception from terminator
        }


        FakeDispatcher dispatcher;
        SubscriptionRouter router;
        MessageDrivenSubscribeTerminator subscribeTerminator;
        Publishers publishers;

        class FakeDispatcher : IMessageDispatcher
        {
            public int FailedNumberOfTimes { get; private set; }

            public List<TransportOperations> DispatchedTransportOperations { get; } = new List<TransportOperations>();

            public Task Dispatch(TransportOperations outgoingMessages, TransportTransaction transaction, CancellationToken cancellationToken = default)
            {
                if (numberOfTimes.HasValue && FailedNumberOfTimes < numberOfTimes.Value)
                {
                    FailedNumberOfTimes++;
                    throw new QueueNotFoundException();
                }

                DispatchedTransportOperations.Add(outgoingMessages);
                return Task.CompletedTask;
            }

            public void FailDispatch(int times)
            {
                numberOfTimes = times;
            }

            int? numberOfTimes;
        }

        class EventA
        {
        }

        class EventB
        {
        }
    }
}