namespace NServiceBus.Core.Tests.Routing
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Extensibility;
    using NServiceBus.Pipeline;
    using NServiceBus.Routing;
    using NServiceBus.Routing.MessageDrivenSubscriptions;
    using Unicast.Queuing;
    using NUnit.Framework;
    using Testing;

    [TestFixture]
    public class MessageDrivenUnsubscribeConnectorTests
    {
        MessageDrivenUnsubscribeConnector terminator;
        SubscriptionRouter router;
        DispatchHelper helper;

        [SetUp]
        public void SetUp()
        {
            var publishers = new Publishers();
            publishers.AddOrReplacePublishers("A", new List<PublisherTableEntry> {new PublisherTableEntry(typeof(object), PublisherAddress.CreateFromPhysicalAddresses("publisher1"))});
            router = new SubscriptionRouter(publishers, new EndpointInstances(), i => i.ToString());
            helper = new DispatchHelper();
            terminator = new MessageDrivenUnsubscribeConnector(router, "replyToAddress", "Endpoint");
        }

        [Test]
        public async Task Should_Dispatch_for_all_publishers()
        {
            await terminator.Invoke(new TestableUnsubscribeContext(), c => helper.Capture(c));

            Assert.AreEqual(1, helper.DispatchedContexts.Count);
        }

        [Test]
        public async Task Should_Dispatch_according_to_max_retries_when_dispatch_fails()
        {
            var options = new UnsubscribeOptions();
            var state = options.GetExtensions().GetOrCreate<MessageDrivenUnsubscribeConnector.Settings>();
            state.MaxRetries = 10;
            state.RetryDelay = TimeSpan.Zero;
            helper.FailDispatch(10);

            var context = new TestableUnsubscribeContext
            {
                Extensions = options.Context
            };

            await terminator.Invoke(context, c => helper.Capture(c));

            Assert.AreEqual(1, helper.DispatchedContexts.Count);
            Assert.AreEqual(10, helper.FailedNumberOfTimes);
        }

        [Test]
        public void Should_Throw_when_max_retries_reached()
        {
            var options = new UnsubscribeOptions();
            var state = options.GetExtensions().GetOrCreate<MessageDrivenUnsubscribeConnector.Settings>();
            state.MaxRetries = 10;
            state.RetryDelay = TimeSpan.Zero;
            helper.FailDispatch(11);

            var context = new TestableUnsubscribeContext
            {
                Extensions = options.Context
            };

            Assert.That(async () => await terminator.Invoke(context, c => helper.Capture(c)), Throws.InstanceOf<QueueNotFoundException>());

            Assert.AreEqual(0, helper.DispatchedContexts.Count);
            Assert.AreEqual(11, helper.FailedNumberOfTimes);
        }

        class DispatchHelper
        {
            int? numberOfTimes;

            public int FailedNumberOfTimes { get; private set; } = 0;

            public List<IDispatchContext> DispatchedContexts { get; } = new List<IDispatchContext>();

            public void FailDispatch(int times)
            {
                numberOfTimes = times;
            }

            public Task Capture(IDispatchContext context)
            {
                if (numberOfTimes.HasValue && FailedNumberOfTimes < numberOfTimes.Value)
                {
                    FailedNumberOfTimes++;
                    throw new QueueNotFoundException();
                }

                DispatchedContexts.Add(context);
                return TaskEx.CompletedTask;
            }
        }
    }
}