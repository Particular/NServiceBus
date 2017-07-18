namespace NServiceBus.Core.Tests.Routing
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
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
            subscribeConnector = new MessageDrivenSubscribeConnector(router, "replyToAddress", "Endpoint");
        }

        [Test]
        public async Task Should_include_TimeSent_and_Version_headers()
        {
            var unsubscribeConnector = new MessageDrivenUnsubscribeConnector(router, "replyToAddress", "Endpoint");

            await subscribeConnector.Invoke(new TestableSubscribeContext(), helper.Capture);
            await unsubscribeConnector.Invoke(new TestableUnsubscribeContext(), c => TaskEx.CompletedTask);

            foreach (var dispatchedTransportOperation in helper.DispatchedTransportOperations)
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
            await subscribeConnector.Invoke(new TestableSubscribeContext(), c => TaskEx.CompletedTask);

            Assert.AreEqual(1, helper.DispatchedTransportOperations.Count);
        }

        [Test]
        public async Task Should_Dispatch_according_to_max_retries_when_dispatch_fails()
        {
            var context = new TestableSubscribeContext();
            var state = context.Extensions.GetOrCreate<MessageDrivenSubscribeConnector.Settings>();
            state.MaxRetries = 10;
            state.RetryDelay = TimeSpan.Zero;
            helper.FailDispatch(10);

            await subscribeConnector.Invoke(context, c => TaskEx.CompletedTask);

            Assert.AreEqual(1, helper.DispatchedTransportOperations.Count);
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
                await subscribeConnector.Invoke(context, c => TaskEx.CompletedTask);
            }, Throws.InstanceOf<QueueNotFoundException>());

            Assert.AreEqual(0, helper.DispatchedTransportOperations.Count);
            Assert.AreEqual(11, helper.FailedNumberOfTimes);
        }

        DispatchHelper helper;
        SubscriptionRouter router;
        MessageDrivenSubscribeConnector subscribeConnector;

        class DispatchHelper
        {
            public int FailedNumberOfTimes { get; private set; }

            public List<TransportOperations> DispatchedTransportOperations { get; } = new List<TransportOperations>();

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

            public void FailDispatch(int times)
            {
                numberOfTimes = times;
            }

            int? numberOfTimes;
        }
    }
}