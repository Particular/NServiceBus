﻿namespace NServiceBus.Core.Tests.Routing
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Extensibility;
    using NServiceBus.Pipeline;
    using NServiceBus.Routing;
    using NServiceBus.Routing.MessageDrivenSubscriptions;
    using NServiceBus.Transports;
    using NServiceBus.Unicast.Queuing;
    using NUnit.Framework;

    [TestFixture]
    public class MessageDrivenSubscribeTerminatorTests
    {
        MessageDrivenSubscribeTerminator terminator;
        SubscriptionRouter router;
        StaticRoutes staticRoutes;
        FakeDispatcher dispatcher;

        [SetUp]
        public void SetUp()
        {
            staticRoutes = new StaticRoutes();
            SetupStaticRoutes();
            router = new SubscriptionRouter(staticRoutes, new[] { typeof(object) });
            dispatcher = new FakeDispatcher();
            terminator = new MessageDrivenSubscribeTerminator(router, "replyToAddress", dispatcher);
        }

        [Test]
        public async Task Should_Dispatch_for_all_publishers()
        {
            await terminator.Invoke(new SubscribeContext(new FakeContext(), typeof(object), new SubscribeOptions()), c => Task.FromResult(0));

            Assert.AreEqual(1, dispatcher.DispatchedOperations.Count);
        }

        [Test]
        public async Task Should_Dispatch_according_to_max_retries_when_dispatch_fails()
        {
            var options = new SubscribeOptions();
            var state = options.GetExtensions().GetOrCreate<MessageDrivenSubscribeTerminator.Settings>();
            state.MaxRetries = 10;
            state.RetryDelay = TimeSpan.Zero;
            dispatcher.FailDispatch(10);

            await terminator.Invoke(new SubscribeContext(new FakeContext(), typeof(object), options), c => Task.FromResult(0));

            Assert.AreEqual(1, dispatcher.DispatchedOperations.Count);
            Assert.AreEqual(10, dispatcher.FailedNumberOfTimes);
        }

        [Test]
        public void Should_Throw_when_max_retries_reached()
        {
            var options = new SubscribeOptions();
            var state = options.GetExtensions().GetOrCreate<MessageDrivenSubscribeTerminator.Settings>();
            state.MaxRetries = 10;
            state.RetryDelay = TimeSpan.Zero;
            dispatcher.FailDispatch(11);

            Assert.Throws<QueueNotFoundException>(async () => await terminator.Invoke(new SubscribeContext(new FakeContext(), typeof(object), options), c => Task.FromResult(0)));

            Assert.AreEqual(0, dispatcher.DispatchedOperations.Count);
            Assert.AreEqual(11, dispatcher.FailedNumberOfTimes);
        }

        private void SetupStaticRoutes()
        {
            staticRoutes.Register(typeof(object), "publisher1");
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