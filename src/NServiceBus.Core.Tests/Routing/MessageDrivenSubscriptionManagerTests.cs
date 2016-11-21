namespace NServiceBus.Core.Tests.Routing
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Extensibility;
    using NServiceBus.Features;
    using NServiceBus.Routing;
    using NServiceBus.Routing.MessageDrivenSubscriptions;
    using Transport;
    using Unicast.Queuing;
    using NUnit.Framework;

    [TestFixture]
    public class MessageDrivenSubscriptionManagerTests
    {
        MessageDrivenSubscriptionManager subscriptionManager;
        SubscriptionRouter router;
        FakeDispatcher dispatcher;

        [SetUp]
        public void SetUp()
        {
            var publishers = new Publishers();
            publishers.AddOrReplacePublishers("A", new List<PublisherTableEntry> {new PublisherTableEntry(typeof(object), PublisherAddress.CreateFromPhysicalAddresses("publisher1"))});
            router = new SubscriptionRouter(publishers, new EndpointInstances(), i => i.ToString());
            dispatcher = new FakeDispatcher();
            subscriptionManager = new MessageDrivenSubscriptionManager("replyToAddress", "Endpoint", dispatcher, router);
        }

        [Test]
        public async Task Unsubscribe_Should_Dispatch_for_all_publishers()
        {
            await subscriptionManager.Unsubscribe(typeof(object), new ContextBag());

            Assert.AreEqual(1, dispatcher.DispatchedTransportOperations.Count);
        }

        [Test]
        public async Task Unsubscribe_Should_Dispatch_according_to_max_retries_when_dispatch_fails()
        {
            var options = new UnsubscribeOptions();
            var state = options.GetExtensions().GetOrCreate<MessageDrivenSubscriptionManager.Settings>();
            state.MaxRetries = 10;
            state.RetryDelay = TimeSpan.Zero;
            dispatcher.FailDispatch(10);

            await subscriptionManager.Unsubscribe(typeof(object), options.Context);

            Assert.AreEqual(1, dispatcher.DispatchedTransportOperations.Count);
            Assert.AreEqual(10, dispatcher.FailedNumberOfTimes);
        }

        [Test]
        public void Unsubscribe_Should_Throw_when_max_retries_reached()
        {
            var options = new UnsubscribeOptions();
            var state = options.GetExtensions().GetOrCreate<MessageDrivenSubscriptionManager.Settings>();
            state.MaxRetries = 10;
            state.RetryDelay = TimeSpan.Zero;
            dispatcher.FailDispatch(11);

            Assert.That(async () => await subscriptionManager.Unsubscribe(typeof(object), options.Context), Throws.InstanceOf<QueueNotFoundException>());

            Assert.AreEqual(0, dispatcher.DispatchedTransportOperations.Count);
            Assert.AreEqual(11, dispatcher.FailedNumberOfTimes);
        }

        [Test]
        public async Task Subscribe_Should_include_TimeSent_and_Version_headers()
        {
            //await subscribeTerminator.Invoke(new TestableSubscribeContext(), c => TaskEx.CompletedTask);
            await subscriptionManager.Subscribe(typeof(object), new ContextBag());
            //            await unsubscribeTerminator.Invoke(new TestableUnsubscribeContext(), c => TaskEx.CompletedTask);

            foreach (var dispatchedTransportOperation in dispatcher.DispatchedTransportOperations)
            {
                var operations = dispatchedTransportOperation.UnicastTransportOperations;

                Assert.IsTrue(operations[0].Message.Headers.ContainsKey(Headers.TimeSent));
                Assert.IsTrue(operations[0].Message.Headers.ContainsKey(Headers.NServiceBusVersion));
            }
        }

        [Test]
        public async Task Subscribe_Should_Dispatch_for_all_publishers()
        {
            await subscriptionManager.Subscribe(typeof(object), new ContextBag());

            Assert.AreEqual(1, dispatcher.DispatchedTransportOperations.Count);
        }

        [Test]
        public async Task Subscribe_Should_Dispatch_according_to_max_retries_when_dispatch_fails()
        {
            var context = new ContextBag();
            var state = context.GetOrCreate<MessageDrivenSubscriptionManager.Settings>();
            state.MaxRetries = 10;
            state.RetryDelay = TimeSpan.Zero;
            dispatcher.FailDispatch(10);

            await subscriptionManager.Subscribe(typeof(object), context);

            Assert.AreEqual(1, dispatcher.DispatchedTransportOperations.Count);
            Assert.AreEqual(10, dispatcher.FailedNumberOfTimes);
        }

        [Test]
        public void Subscribe_Should_Throw_when_max_retries_reached()
        {
            var context = new ContextBag();
            var state = context.GetOrCreate<MessageDrivenSubscriptionManager.Settings>();
            state.MaxRetries = 10;
            state.RetryDelay = TimeSpan.Zero;
            dispatcher.FailDispatch(11);

            Assert.That(async () =>
            {
                await subscriptionManager.Subscribe(typeof(object), context);
            }, Throws.InstanceOf<QueueNotFoundException>());

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