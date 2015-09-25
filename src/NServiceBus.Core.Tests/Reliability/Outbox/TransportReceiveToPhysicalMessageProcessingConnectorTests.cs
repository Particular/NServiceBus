namespace NServiceBus.Core.Tests.Reliability.Outbox
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Threading.Tasks;
    using NServiceBus.DelayedDelivery;
    using NServiceBus.DeliveryConstraints;
    using NServiceBus.Outbox;
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
        public async Task Should_honor_stored_delivery_constraints_and_routing_strategies()
        {
            var options = new Dictionary<string, string>();
            var deliverTime = DateTime.UtcNow.AddDays(1);

            new DirectToTargetDestination("myEndpoint").Serialize(options);
            new NonDurableDelivery().Serialize(options);
            new DelayDeliveryWith(TimeSpan.FromSeconds(10)).Serialize(options);
            new DoNotDeliverBefore(deliverTime).Serialize(options);

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
            
            var directRouting = fakeBatchPipeline.TransportOperations.First().DispatchOptions.RoutingStrategy as DirectToTargetDestination;
            Assert.NotNull(directRouting);
            Assert.AreEqual("myEndpoint", directRouting.Destination);

            Assert.Null(fakeOutbox.StoredMessage);
        }

        static TransportReceiveContext CreateContext()
        {
            var context = new TransportReceiveContext(new IncomingMessage("id", new Dictionary<string, string>(), new MemoryStream()), new RootContext(null));
            return context;
        }

        

        //[Test]
        //public void DeserializeDiscardIfNotReceivedBefore()
        //{
        //    var options = new Dictionary<string, string>();

        //    var delay = TimeSpan.Parse("00:10:00");

        //    new DiscardIfNotReceivedBefore(delay).Serialize(options);

        //    DiscardIfNotReceivedBefore constraint;

        //    new DeliveryConstraintsFactory().DeserializeConstraints(options).TryGet(out constraint);

        //    Assert.AreEqual(delay, constraint.MaxTime);
        //}



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