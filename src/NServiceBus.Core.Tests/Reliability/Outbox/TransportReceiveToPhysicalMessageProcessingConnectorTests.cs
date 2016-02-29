namespace NServiceBus.Core.Tests.Reliability.Outbox
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using DelayedDelivery;
    using DeliveryConstraints;
    using NServiceBus.Outbox;
    using NServiceBus.Performance.TimeToBeReceived;
    using NServiceBus.Pipeline;
    using NServiceBus.Routing;
    using NServiceBus.Transports;
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

            var context = CreateContext(fakeBatchPipeline);

            await Invoke(context);

            Assert.True(fakeBatchPipeline.TransportOperations.First().DeliveryConstraints.Any(c => c is NonDurableDelivery));

            DelayDeliveryWith delayDeliveryWith;

            Assert.True(fakeBatchPipeline.TransportOperations.First().DeliveryConstraints.TryGet(out delayDeliveryWith));
            Assert.AreEqual(TimeSpan.FromSeconds(10), delayDeliveryWith.Delay);

            DoNotDeliverBefore doNotDeliverBefore;

            Assert.True(fakeBatchPipeline.TransportOperations.First().DeliveryConstraints.TryGet(out doNotDeliverBefore));
            Assert.AreEqual(deliverTime.ToString(), doNotDeliverBefore.At.ToString());

            DiscardIfNotReceivedBefore discard;

            Assert.True(fakeBatchPipeline.TransportOperations.First().DeliveryConstraints.TryGet(out discard));
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

            var context = CreateContext(fakeBatchPipeline);

            await Invoke(context);

            var routing = fakeBatchPipeline.TransportOperations.First().AddressTag as UnicastAddressTag;
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

            var context = CreateContext(fakeBatchPipeline);

            await Invoke(context);

            var routing = fakeBatchPipeline.TransportOperations.First().AddressTag as MulticastAddressTag;
            Assert.NotNull(routing);
            Assert.AreEqual(typeof(MyEvent), routing.MessageType);
            Assert.Null(fakeOutbox.StoredMessage);
        }

        static ITransportReceiveContext CreateContext(FakeBatchPipeline pipeline)
        {
            var context = new TransportReceiveContext("id", new Dictionary<string, string>(), new MemoryStream(), null, new CancellationTokenSource(), new RootContext(null, new FakePipelineCache(pipeline)));
            return context;
        }

        [SetUp]
        public void SetUp()
        {
            fakeOutbox = new FakeOutboxStorage();
            fakeBatchPipeline = new FakeBatchPipeline();

            behavior = new TransportReceiveToPhysicalMessageProcessingConnector(fakeOutbox);
        }

        async Task Invoke(ITransportReceiveContext context)
        {
            await behavior.Invoke(context, c => TaskEx.CompletedTask).ConfigureAwait(false);
        }

        FakeBatchPipeline fakeBatchPipeline;
        FakeOutboxStorage fakeOutbox;
        TransportReceiveToPhysicalMessageProcessingConnector behavior;

        class MyEvent { }

        class FakePipelineCache : IPipelineCache
        {
            IPipeline<IBatchDispatchContext> pipeline;

            public FakePipelineCache(IPipeline<IBatchDispatchContext> pipeline)
            {
                this.pipeline = pipeline;
            }

            public IPipeline<TContext> Pipeline<TContext>()
                where TContext : IBehaviorContext

            {
                return (IPipeline<TContext>)pipeline;
            }
        }

        class FakeBatchPipeline : IPipeline<IBatchDispatchContext>
        {
            public IEnumerable<NServiceBus.Transports.TransportOperation> TransportOperations { get; set; }

            public Task Invoke(IBatchDispatchContext context)
            {
                TransportOperations = context.Operations;

                return TaskEx.CompletedTask;
            }
        }
    }
}