﻿namespace NServiceBus.Core.Tests.Routing
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Extensibility;
    using NServiceBus.Routing;
    using NServiceBus.Routing.MessageDrivenSubscriptions;
    using Transport;
    using Unicast.Queuing;
    using NUnit.Framework;
    using Testing;

    [TestFixture]
    public class MessageDrivenUnsubscribeTerminatorTests
    {
        MessageDrivenUnsubscribeTerminator terminator;
        SubscriptionRouter router;
        FakeDispatcher dispatcher;

        [SetUp]
        public void SetUp()
        {
            var publishers = new Publishers();
            publishers.AddOrReplacePublishers(Guid.NewGuid(), new List<PublisherTableEntry> {new PublisherTableEntry(typeof(object), PublisherAddress.CreateFromPhysicalAddresses("publisher1"))});
            router = new SubscriptionRouter(publishers, new EndpointInstances(), i => i.ToString());
            dispatcher = new FakeDispatcher();
            terminator = new MessageDrivenUnsubscribeTerminator(router, "replyToAddress", "Endpoint", dispatcher);
        }

        [Test]
        public async Task Should_Dispatch_for_all_publishers()
        {
            await terminator.Invoke(new TestableUnsubscribeContext(), c => TaskEx.CompletedTask);

            Assert.AreEqual(1, dispatcher.DispatchedTransportOperations.Count);
        }

        [Test]
        public async Task Should_Dispatch_according_to_max_retries_when_dispatch_fails()
        {
            var options = new UnsubscribeOptions();
            var state = options.GetExtensions().GetOrCreate<MessageDrivenUnsubscribeTerminator.Settings>();
            state.MaxRetries = 10;
            state.RetryDelay = TimeSpan.Zero;
            dispatcher.FailDispatch(10);

            var context = new TestableUnsubscribeContext
            {
                Extensions = options.Context
            };

            await terminator.Invoke(context, c => TaskEx.CompletedTask);

            Assert.AreEqual(1, dispatcher.DispatchedTransportOperations.Count);
            Assert.AreEqual(10, dispatcher.FailedNumberOfTimes);
        }

        [Test]
        public void Should_Throw_when_max_retries_reached()
        {
            var options = new UnsubscribeOptions();
            var state = options.GetExtensions().GetOrCreate<MessageDrivenUnsubscribeTerminator.Settings>();
            state.MaxRetries = 10;
            state.RetryDelay = TimeSpan.Zero;
            dispatcher.FailDispatch(11);

            var context = new TestableUnsubscribeContext
            {
                Extensions = options.Context
            };

            Assert.That(async () => await terminator.Invoke(context, c => TaskEx.CompletedTask), Throws.InstanceOf<QueueNotFoundException>());

            Assert.AreEqual(0, dispatcher.DispatchedTransportOperations.Count);
            Assert.AreEqual(11, dispatcher.FailedNumberOfTimes);
        }

        class FakeDispatcher : IDispatchMessages
        {
            int? numberOfTimes;

            public int FailedNumberOfTimes { get; private set; } = 0;

            public List<TransportOperations> DispatchedTransportOperations { get; } = new List<TransportOperations>();

            public void FailDispatch(int times)
            {
                numberOfTimes = times;
            }

            public Task Dispatch(TransportOperations outgoingMessages, TransportTransaction transaction, ContextBag context)
            {
                if (numberOfTimes.HasValue && FailedNumberOfTimes < numberOfTimes.Value)
                {
                    FailedNumberOfTimes++;
                    throw new QueueNotFoundException();
                }

                DispatchedTransportOperations.Add(outgoingMessages);
                return TaskEx.CompletedTask;
            }
        }
    }
}