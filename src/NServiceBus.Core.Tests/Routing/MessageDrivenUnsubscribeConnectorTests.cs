namespace NServiceBus.Core.Tests.Routing
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Extensibility;
    using NServiceBus.Pipeline;
    using NServiceBus.Routing;
    using NServiceBus.Routing.MessageDrivenSubscriptions;
    using Transport;
    using Unicast.Queuing;
    using NUnit.Framework;
    using Testing;

    [TestFixture]
    public class MessageDrivenUnsubscribeConnectorTests
    {
        MessageDrivenUnsubscribeConnector connector;
        SubscriptionRouter router;
        DispatchHelper helper;

        [SetUp]
        public void SetUp()
        {
            var publishers = new Publishers();
            publishers.AddOrReplacePublishers("A", new List<PublisherTableEntry> {new PublisherTableEntry(typeof(object), PublisherAddress.CreateFromPhysicalAddresses("publisher1"))});
            router = new SubscriptionRouter(publishers, new EndpointInstances(), i => i.ToString());
            helper = new DispatchHelper();
            connector = new MessageDrivenUnsubscribeConnector(router, "replyToAddress", "Endpoint");
        }

        [Test]
        public async Task Should_Dispatch_for_all_publishers()
        {
            await connector.Invoke(new TestableUnsubscribeContext(), helper.Capture);

            Assert.AreEqual(1, helper.DispatchedTransportOperations.Count);
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

            await connector.Invoke(context, helper.Capture);

            Assert.AreEqual(1, helper.DispatchedTransportOperations.Count);
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

            Assert.That(async () => await connector.Invoke(context, helper.Capture), Throws.InstanceOf<QueueNotFoundException>());

            Assert.AreEqual(0, helper.DispatchedTransportOperations.Count);
            Assert.AreEqual(11, helper.FailedNumberOfTimes);
        }

        class DispatchHelper
        {
            int? numberOfTimes;

            public int FailedNumberOfTimes { get; private set; } = 0;

            public List<TransportOperations> DispatchedTransportOperations { get; } = new List<TransportOperations>();

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

                DispatchedTransportOperations.Add(new TransportOperations(context.Operations.ToArray()));
                return TaskEx.CompletedTask;
            }
        }
    }
}