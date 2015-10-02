namespace NServiceBus.Core.Tests.Reliability.Outbox
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Threading.Tasks;
    using DelayedDelivery;
    using DeliveryConstraints;
    using NServiceBus.Outbox;
    using NServiceBus.Performance.TimeToBeReceived;
    using NServiceBus.Pipeline;
    using NServiceBus.Pipeline.Contexts;
    using NServiceBus.Routing;
    using Transports;
    using NUnit.Framework;
    using TransportOperation = NServiceBus.Outbox.TransportOperation;

    [TestFixture]
    public class TransportReceiveToPhysicalMessageProcessingConnectorTests
    {

        [Test]
        public async Task Should_honor_stored_delivery_constraints()
        {
            var options = new Dictionary<string, string>();
            var deliverTime = DateTime.UtcNow.AddDays(1);
            var maxTime = TimeSpan.FromDays(1);

            options["Destination"] = "test";

            options["NonDurable"] = true.ToString();
            options["DeliverAt"] = DateTimeExtensions.ToWireFormattedString(deliverTime);
            options["DelayDeliveryFor"] = TimeSpan.FromSeconds(10).ToString();
            options["TimeToBeReceived"] = maxTime.ToString();

            fakeOutbox.ExistingMessage = new OutboxMessage("id", new List<TransportOperation>
            {
                new TransportOperation("x",options,new byte[0],new Dictionary<string, string>())
            });

            var context = CreateContext();

            await Invoke(context);

            Assert.True(fakeBatchPipeline.TransportOperations.First().DispatchOptions.DeliveryConstraints.Any(c => c is NonDurableDelivery));

            DelayDeliveryWith delayDeliveryWith;

            Assert.True(fakeBatchPipeline.TransportOperations.First().DispatchOptions.DeliveryConstraints.TryGet(out delayDeliveryWith));
            Assert.AreEqual(TimeSpan.FromSeconds(10), delayDeliveryWith.Delay);

            DoNotDeliverBefore doNotDeliverBefore;

            Assert.True(fakeBatchPipeline.TransportOperations.First().DispatchOptions.DeliveryConstraints.TryGet(out doNotDeliverBefore));
            Assert.AreEqual(deliverTime.ToString(), doNotDeliverBefore.At.ToString());

            DiscardIfNotReceivedBefore discard;

            Assert.True(fakeBatchPipeline.TransportOperations.First().DispatchOptions.DeliveryConstraints.TryGet(out discard));
            Assert.AreEqual(maxTime, discard.MaxTime);

            Assert.Null(fakeOutbox.StoredMessage);
        }

        [Test]
        public async Task Should_honor_stored_direct_routing()
        {
            var options = new Dictionary<string, string>();

            options["Destination"] = "myEndpoint";
            
            fakeOutbox.ExistingMessage = new OutboxMessage("id", new List<TransportOperation>
            {
                new TransportOperation("x",options,new byte[0],new Dictionary<string, string>())
            });

            var context = CreateContext();

            await Invoke(context);

            var routing = fakeBatchPipeline.TransportOperations.First().DispatchOptions.RoutingStrategy as DirectToTargetDestination;
            Assert.NotNull(routing);
            Assert.AreEqual("myEndpoint", routing.Destination);
            Assert.Null(fakeOutbox.StoredMessage);
        }


        [Test]
        public async Task Should_honor_stored_pubsub_routing()
        {
            var options = new Dictionary<string, string>();

            options["EventType"] = typeof(MyEvent).AssemblyQualifiedName;
            
            fakeOutbox.ExistingMessage = new OutboxMessage("id", new List<TransportOperation>
            {
                new TransportOperation("x",options,new byte[0],new Dictionary<string, string>())
            });

            var context = CreateContext();

            await Invoke(context);

            var routing = fakeBatchPipeline.TransportOperations.First().DispatchOptions.RoutingStrategy as ToAllSubscribers;
            Assert.NotNull(routing);
            Assert.AreEqual(typeof(MyEvent), routing.EventType);
            Assert.Null(fakeOutbox.StoredMessage);
        }

        static TransportReceiveContext CreateContext()
        {
            var context = new TransportReceiveContext(new IncomingMessage("id", new Dictionary<string, string>(), new MemoryStream()), new RootContext(null));
            return context;
        }

        [SetUp]
        public void SetUp()
        {
            fakeOutbox = new FakeOutboxStorage();
            fakeBatchPipeline = new FakeBatchPipeline();

            behavior = new TransportReceiveToPhysicalMessageProcessingConnector(fakeBatchPipeline, fakeOutbox);
        }

        async Task Invoke(TransportReceiveContext context)
        {
            await behavior.Invoke(context, c => TaskEx.Completed).ConfigureAwait(false);
        }

        FakeBatchPipeline fakeBatchPipeline;
        FakeOutboxStorage fakeOutbox;
        TransportReceiveToPhysicalMessageProcessingConnector behavior;

        class MyEvent { }
        class FakeBatchPipeline : IPipelineBase<BatchDispatchContext>
        {
            public IEnumerable<Transports.TransportOperation> TransportOperations { get; set; }

            public Task Invoke(BatchDispatchContext context)
            {
                TransportOperations = context.Operations;

                return TaskEx.Completed;
            }
        }
    }
}