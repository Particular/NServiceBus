namespace NServiceBus.Core.Tests.Reliability.Outbox
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Diagnostics;
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
            var options = new DispatchProperties();
            var deliverTime = DateTimeOffset.UtcNow.AddDays(1);
            var maxTime = TimeSpan.FromDays(1);

            options["Destination"] = "test";

            options["DeliverAt"] = DateTimeOffsetHelper.ToWireFormattedString(deliverTime);
            options["DelayDeliveryFor"] = TimeSpan.FromSeconds(10).ToString();
            options["TimeToBeReceived"] = maxTime.ToString();

            fakeOutbox.ExistingMessage = new OutboxMessage(messageId, new[]
            {
                new NServiceBus.Outbox.TransportOperation("x", options, new byte[0], [])
            });

            var context = CreateContext(fakeBatchPipeline, messageId);

            await Invoke(context);

            var operationProperties = new DispatchProperties(fakeBatchPipeline.TransportOperations.First().Properties);
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
            var properties = new DispatchProperties { ["Destination"] = "myEndpoint" };


            fakeOutbox.ExistingMessage = new OutboxMessage(messageId, new[]
            {
                new NServiceBus.Outbox.TransportOperation("x", properties, new byte[0], [])
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
            var properties = new DispatchProperties
            {
                ["EventType"] = typeof(MyEvent).AssemblyQualifiedName
            };


            fakeOutbox.ExistingMessage = new OutboxMessage(messageId, new[]
            {
                new NServiceBus.Outbox.TransportOperation("x", properties, new byte[0], [])
            });

            var context = CreateContext(fakeBatchPipeline, messageId);

            await Invoke(context);

            var routing = fakeBatchPipeline.TransportOperations.First().AddressTag as MulticastAddressTag;
            Assert.NotNull(routing);
            Assert.AreEqual(typeof(MyEvent), routing.MessageType);
            Assert.Null(fakeOutbox.StoredMessage);
        }

        [Test]
        public async Task Should_add_outbox_span_tag_when_deduplicating()
        {
            string messageId = Guid.NewGuid().ToString();
            fakeOutbox.ExistingMessage = new OutboxMessage(messageId, Array.Empty<NServiceBus.Outbox.TransportOperation>());
            var context = CreateContext(fakeBatchPipeline, messageId);

            using var pipelineActivity = new Activity("test activity");
            pipelineActivity.Start();
            context.Extensions.SetIncomingPipelineActitvity(pipelineActivity);

            await Invoke(context);

            Assert.AreEqual(true, pipelineActivity.TagObjects.ToImmutableDictionary()["nservicebus.outbox.deduplicate-message"]);
        }

        [Test]
        public async Task Should_add_batch_dispatch_events_when_sending_batched_messages()
        {
            var context = CreateContext(fakeBatchPipeline, Guid.NewGuid().ToString());

            using var pipelineActivity = new Activity("test activity");
            pipelineActivity.Start();
            context.Extensions.SetIncomingPipelineActitvity(pipelineActivity);

            await Invoke(context, c =>
            {
                var batchedSends = c.Extensions.Get<PendingTransportOperations>();
                batchedSends.AddRange(new TransportOperation[]
                {
                    new TransportOperation(new OutgoingMessage(Guid.NewGuid().ToString(), [], Array.Empty<byte>()), new UnicastAddressTag("destination")),
                    new TransportOperation(new OutgoingMessage(Guid.NewGuid().ToString(), [], Array.Empty<byte>()), new UnicastAddressTag("destination")),
                    new TransportOperation(new OutgoingMessage(Guid.NewGuid().ToString(), [], Array.Empty<byte>()), new UnicastAddressTag("destination"))
                });
                return Task.CompletedTask;
            });

            var startDispatchErActivityEventsvent = pipelineActivity.Events.Where(e => e.Name == "Start dispatching").ToArray();
            Assert.AreEqual(1, startDispatchErActivityEventsvent.Count());
            Assert.AreEqual(3, startDispatchErActivityEventsvent.Single().Tags.ToImmutableDictionary()["message-count"]);
            Assert.AreEqual(1, pipelineActivity.Events.Count(e => e.Name == "Finished dispatching"));
        }

        [Test]
        public async Task Should_not_add_batch_dispatch_events_when_no_batched_messages()
        {
            var context = CreateContext(fakeBatchPipeline, Guid.NewGuid().ToString());

            using var pipelineActivity = new Activity("test activity");
            pipelineActivity.Start();
            context.Extensions.SetIncomingPipelineActitvity(pipelineActivity);

            await Invoke(context);

            Assert.AreEqual(0, pipelineActivity.Events.Count(e => e.Name == "Start dispatching"));
            Assert.AreEqual(0, pipelineActivity.Events.Count(e => e.Name == "Finished dispatching"));
        }

        static ITransportReceiveContext CreateContext(FakeBatchPipeline pipeline, string messageId)
        {
            var context = new TestableTransportReceiveContext
            {
                Message = new IncomingMessage(messageId, [], new byte[0])
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

        Task Invoke(ITransportReceiveContext context, Func<IIncomingPhysicalMessageContext, Task> next = null) => behavior.Invoke(context, next ?? (_ => Task.CompletedTask));

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