namespace NServiceBus.Core.Tests.Routing
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using NServiceBus.Pipeline;
    using NServiceBus.Routing;
    using NServiceBus.Routing.MessageDrivenSubscriptions;
    using Transport;
    using NUnit.Framework;
    using Testing;
    using Unicast.Queuing;

    [TestFixture]
    public class MessageDrivenSubscribeConnectorTests
    {
        [SetUp]
        public void SetUp()
        {
            var publishers = new Publishers();
            publishers.AddOrReplacePublishers("A", new List<PublisherTableEntry> {new PublisherTableEntry(typeof(object), PublisherAddress.CreateFromPhysicalAddresses("publisher1"))});
            router = new SubscriptionRouter(publishers, new EndpointInstances(), i => i.ToString());
            helper = new DispatchHelper();
            subscribeTerminator = new MessageDrivenSubscribeConnector(router, "replyToAddress", "Endpoint");
        }

        [Test]
        public async Task Should_include_TimeSent_and_Version_headers()
        {
            var unsubscribeTerminator = new MessageDrivenUnsubscribeConnector(router, "replyToAddress", "Endpoint");

            await subscribeTerminator.Invoke(new TestableSubscribeContext(), c => helper.Capture(c));
            await unsubscribeTerminator.Invoke(new TestableUnsubscribeContext(), c => helper.Capture(c));

            foreach (var dispatchContext in helper.DispatchedContexts)
            {
                var operations = new List<TransportOperation>(dispatchContext.Operations);

                Assert.IsTrue(operations[0].Message.Headers.ContainsKey(Headers.TimeSent));
                Assert.IsTrue(operations[0].Message.Headers.ContainsKey(Headers.NServiceBusVersion));
            }
        }

        [Test]
        public async Task Should_Dispatch_for_all_publishers()
        {
            await subscribeTerminator.Invoke(new TestableSubscribeContext(), c => helper.Capture(c));

            Assert.AreEqual(1, helper.DispatchedContexts.Count);
        }

        [Test]
        public async Task Should_Dispatch_according_to_max_retries_when_dispatch_fails()
        {
            var context = new TestableSubscribeContext();
            var state = context.Extensions.GetOrCreate<MessageDrivenSubscribeConnector.Settings>();
            state.MaxRetries = 10;
            state.RetryDelay = TimeSpan.Zero;
            helper.FailDispatch(10);

            await subscribeTerminator.Invoke(context, c => helper.Capture(c));

            Assert.AreEqual(1, helper.DispatchedContexts.Count);
            Assert.AreEqual(10, helper.FailedNumberOfTimes);
        }

        [Test]
        public void Should_Throw_when_max_retries_reached()
        {
            var context = new TestableSubscribeContext();
            var state = context.Extensions.GetOrCreate<MessageDrivenSubscribeConnector.Settings>();
            state.MaxRetries = 10;
            state.RetryDelay = TimeSpan.Zero;
            helper.FailDispatch(11);

            Assert.That(async () =>
            {
                await subscribeTerminator.Invoke(context, c => helper.Capture(c));
            }, Throws.InstanceOf<QueueNotFoundException>());

            Assert.AreEqual(0, helper.DispatchedContexts.Count);
            Assert.AreEqual(11, helper.FailedNumberOfTimes);
        }

        DispatchHelper helper;
        SubscriptionRouter router;
        MessageDrivenSubscribeConnector subscribeTerminator;

        class DispatchHelper
        {
            public int FailedNumberOfTimes { get; private set; }

            public List<IDispatchContext> DispatchedContexts { get; } = new List<IDispatchContext>();

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

            public void FailDispatch(int times)
            {
                numberOfTimes = times;
            }

            int? numberOfTimes;
        }
    }
}