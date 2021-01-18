namespace NServiceBus.Core.Tests.Reliability.Outbox
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using NServiceBus.Outbox;
    using NServiceBus.Pipeline;
    using NServiceBus.Routing;
    using NUnit.Framework;
    using Testing;
    using Transport;
    using TransportOperation = Transport.TransportOperation;

    [TestFixture]
    public class TransportReceiveToPhysicalMessageConnectorTests
    {
        [Test]
        public async Task Should_honor_stored_delivery_constraints()
        {
            var messageId = "id";
            var options = new OperationProperties();
            var deliverTime = DateTimeOffset.UtcNow.AddDays(1);
            var maxTime = TimeSpan.FromDays(1);

            options["Destination"] = "test";

            options["DeliverAt"] = DateTimeOffsetHelper.ToWireFormattedString(deliverTime);
            options["DelayDeliveryFor"] = TimeSpan.FromSeconds(10).ToString();
            options["TimeToBeReceived"] = maxTime.ToString();

            fakeOutbox.ExistingMessage = new OutboxMessage(messageId, new[]
            {
                new NServiceBus.Outbox.TransportOperation("x", options, new byte[0], new Dictionary<string, string>())
            });

            var context = CreateContext(fakeBatchPipeline, messageId);

            await Invoke(context);

            var operationProperties = new OperationProperties(fakeBatchPipeline.TransportOperations.First().Properties);
            var delayDeliveryWith = operationProperties.DelayDeliveryWith;
            Assert.NotNull(delayDeliveryWith);
            Assert.AreEqual(TimeSpan.FromSeconds(10), delayDeliveryWith.Delay);

            var doNotDeliverBefore = operationProperties.DoNotDeliverBefore;
            Assert.NotNull(doNotDeliverBefore);
            Assert.AreEqual(deliverTime.ToString(), doNotDeliverBefore.At.ToString());

            var discard = operationProperties.DiscardIfNotReceivedBefore;
            Assert.NotNull(discard);
            Assert.AreEqual(maxTime, discard.MaxTime);

            Assert.Null(fakeOutbox.StoredMessage);
        }

        [Test]
        public async Task Should_honor_stored_direct_routing()
        {
            var messageId = "id";
            var options = new OperationProperties();

            options["Destination"] = "myEndpoint";

            fakeOutbox.ExistingMessage = new OutboxMessage(messageId, new[]
            {
                new NServiceBus.Outbox.TransportOperation("x", options, new byte[0], new Dictionary<string, string>())
            });

            var context = CreateContext(fakeBatchPipeline, messageId);

            await Invoke(context);

            var routing = fakeBatchPipeline.TransportOperations.First().AddressTag as UnicastAddressTag;
            Assert.NotNull(routing);
            Assert.AreEqual("myEndpoint", routing.Destination);
            Assert.Null(fakeOutbox.StoredMessage);
        }


        [Test]
        public async Task Should_honor_stored_pubsub_routing()
        {
            var messageId = "id";
            var options = new OperationProperties();

            options["EventType"] = typeof(MyEvent).AssemblyQualifiedName;

            fakeOutbox.ExistingMessage = new OutboxMessage(messageId, new[]
            {
                new NServiceBus.Outbox.TransportOperation("x", options, new byte[0], new Dictionary<string, string>())
            });

            var context = CreateContext(fakeBatchPipeline, messageId);

            await Invoke(context);

            var routing = fakeBatchPipeline.TransportOperations.First().AddressTag as MulticastAddressTag;
            Assert.NotNull(routing);
            Assert.AreEqual(typeof(MyEvent), routing.MessageType);
            Assert.Null(fakeOutbox.StoredMessage);
        }

        static ITransportReceiveContext CreateContext(FakeBatchPipeline pipeline, string messageId)
        {
            var context = new TestableTransportReceiveContext
            {
                Message = new IncomingMessage(messageId, new Dictionary<string, string>(), new byte[0])
            };

            context.Extensions.Set<IPipelineCache>(new FakePipelineCache(pipeline));

            return context;
        }

        [SetUp]
        public void SetUp()
        {
            fakeOutbox = new FakeOutboxStorage();
            fakeBatchPipeline = new FakeBatchPipeline();

            behavior = new TransportReceiveToPhysicalMessageConnector(fakeOutbox);
        }

        Task Invoke(ITransportReceiveContext context)
        {
            return behavior.Invoke(context, c => Task.CompletedTask);
        }

        TransportReceiveToPhysicalMessageConnector behavior;

        FakeBatchPipeline fakeBatchPipeline;
        FakeOutboxStorage fakeOutbox;

        class MyEvent
        {
        }

        class FakePipelineCache : IPipelineCache
        {
            public FakePipelineCache(IPipeline<IBatchDispatchContext> pipeline)
            {
                this.pipeline = pipeline;
            }

            public IPipeline<TContext> Pipeline<TContext>()
                where TContext : IBehaviorContext
            {
                return (IPipeline<TContext>)pipeline;
            }

            IPipeline<IBatchDispatchContext> pipeline;
        }

        class FakeBatchPipeline : IPipeline<IBatchDispatchContext>
        {
            public IEnumerable<TransportOperation> TransportOperations { get; set; }

            public Task Invoke(IBatchDispatchContext context)
            {
                TransportOperations = context.Operations;

                return Task.CompletedTask;
            }
        }
    }
}