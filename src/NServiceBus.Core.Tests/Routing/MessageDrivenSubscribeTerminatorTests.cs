using System.Threading;
using NServiceBus.Transports;

namespace NServiceBus.Core.Tests.Routing
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Extensibility;
    using NServiceBus.Routing;
    using NServiceBus.Routing.MessageDrivenSubscriptions;
    using Transport;
    using NUnit.Framework;
    using Testing;
    using Unicast.Queuing;

    [TestFixture]
    public class MessageDrivenSubscribeTerminatorTests
    {
        [SetUp]
        public void SetUp()
        {
            var publishers = new Publishers();
            publishers.AddOrReplacePublishers("A", new List<PublisherTableEntry> {new PublisherTableEntry(typeof(object), PublisherAddress.CreateFromPhysicalAddresses("publisher1"))});
            router = new SubscriptionRouter(publishers, new EndpointInstances(), i => i.ToString());
            dispatcher = new FakeDispatcher();
            subscribeTerminator = new MessageDrivenSubscribeTerminator(router, "replyToAddress", "Endpoint", dispatcher);
        }

        [Test]
        public async Task Should_include_TimeSent_and_Version_headers()
        {
            var unsubscribeTerminator = new MessageDrivenUnsubscribeTerminator(router, "replyToAddress", "Endpoint", dispatcher);

            await subscribeTerminator.Invoke(new TestableSubscribeContext(), c => Task.CompletedTask);
            await unsubscribeTerminator.Invoke(new TestableUnsubscribeContext(), c => Task.CompletedTask);

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
            await subscribeTerminator.Invoke(new TestableSubscribeContext(), c => Task.CompletedTask);

            Assert.AreEqual(1, dispatcher.DispatchedTransportOperations.Count);
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

        FakeDispatcher dispatcher;
        SubscriptionRouter router;
        MessageDrivenSubscribeTerminator subscribeTerminator;

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
    }
}