namespace NServiceBus.Core.Tests.Routing
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using NServiceBus.Extensibility;
    using NServiceBus.Pipeline;
    using NServiceBus.Routing;
    using NServiceBus.Routing.MessageDrivenSubscriptions;
    using NServiceBus.Transports;
    using NServiceBus.Unicast.Queuing;
    using NUnit.Framework;

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
            publishers.AddStatic("publisher1",typeof(object));
            router = new SubscriptionRouter(publishers, new KnownEndpoints(), new TransportAddresses());
            dispatcher = new FakeDispatcher();
            terminator = new MessageDrivenUnsubscribeTerminator(router, "replyToAddress", dispatcher);
        }

        [Test]
        public async Task Should_Dispatch_for_all_publishers()
        {
            await terminator.Invoke(new UnsubscribeContext(new FakeContext(), typeof(object), new UnsubscribeOptions()), c => Task.FromResult(0));

            Assert.AreEqual(1, dispatcher.DispatchedOperations.Count);
        }

        [Test]
        public async Task Should_Dispatch_according_to_max_retries_when_dispatch_fails()
        {
            var options = new UnsubscribeOptions();
            var state = options.GetExtensions().GetOrCreate<MessageDrivenUnsubscribeTerminator.Settings>();
            state.MaxRetries = 10;
            state.RetryDelay = TimeSpan.Zero;
            dispatcher.FailDispatch(10);

            await terminator.Invoke(new UnsubscribeContext(new FakeContext(), typeof(object), options), c => Task.FromResult(0));

            Assert.AreEqual(1, dispatcher.DispatchedOperations.Count);
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

            Assert.Throws<QueueNotFoundException>(async () => await terminator.Invoke(new UnsubscribeContext(new FakeContext(), typeof(object), options), c => Task.FromResult(0)));

            Assert.AreEqual(0, dispatcher.DispatchedOperations.Count);
            Assert.AreEqual(11, dispatcher.FailedNumberOfTimes);
        }

        class FakeDispatcher : IDispatchMessages
        {
            int? numberOfTimes;

            public FakeDispatcher()
            {
                DispatchedOperations = new List<IEnumerable<TransportOperation>>();
            }

            public int FailedNumberOfTimes { get; private set; } = 0;

            public List<IEnumerable<TransportOperation>> DispatchedOperations { get; }

            public void FailDispatch(int times)
            {
                numberOfTimes = times;
            }

            public Task Dispatch(IEnumerable<TransportOperation> outgoingMessages, ReadOnlyContextBag context)
            {
                if (numberOfTimes.HasValue && FailedNumberOfTimes < numberOfTimes.Value)
                {
                    FailedNumberOfTimes++;
                    throw new QueueNotFoundException();
                }

                DispatchedOperations.Add(outgoingMessages);
                return Task.FromResult(0);
            }
        }

        class FakeContext : BehaviorContext
        {
            public FakeContext() : base(null)
            {
            }
        }
    }
}