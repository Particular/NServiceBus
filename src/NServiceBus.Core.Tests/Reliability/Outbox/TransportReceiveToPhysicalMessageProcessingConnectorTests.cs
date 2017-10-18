namespace NServiceBus.Core.Tests.Reliability.Outbox
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using DelayedDelivery;
    using DeliveryConstraints;
    using NServiceBus.Outbox;
    using NServiceBus.Performance.TimeToBeReceived;
    using NServiceBus.Pipeline;
    using NServiceBus.Routing;
    using NUnit.Framework;
    using Testing;
    using Transport;
    using TransportOperation = Transport.TransportOperation;

    [TestFixture]
    public class TransportReceiveToPhysicalMessageProcessingConnectorTests
    {
        [Test]
        public async Task Should_honor_stored_delivery_constraints()
        {
            var messageId = "id";
            var options = new Dictionary<string, string>();
            var deliverTime = DateTime.UtcNow.AddDays(1);
            var maxTime = TimeSpan.FromDays(1);

            options["Destination"] = "test";

            options["NonDurable"] = true.ToString();
            options["DeliverAt"] = DateTimeExtensions.ToWireFormattedString(deliverTime);
            options["DelayDeliveryFor"] = TimeSpan.FromSeconds(10).ToString();
            options["TimeToBeReceived"] = maxTime.ToString();

            fakeOutbox.ExistingMessage = new OutboxMessage(messageId, new[]
            {
                new NServiceBus.Outbox.TransportOperation("x", options, new byte[0], new Dictionary<string, string>())
            });

            var context = CreateContext(fakeBatchPipeline, messageId);

            await Invoke(context);

            Assert.True(fakeBatchPipeline.TransportOperations.First().DeliveryConstraints.Any(c => c is NonDurableDelivery));

            Assert.True(fakeBatchPipeline.TransportOperations.First().DeliveryConstraints.TryGet(out DelayDeliveryWith delayDeliveryWith));
            Assert.AreEqual(TimeSpan.FromSeconds(10), delayDeliveryWith.Delay);

            Assert.True(fakeBatchPipeline.TransportOperations.First().DeliveryConstraints.TryGet(out DoNotDeliverBefore doNotDeliverBefore));
            Assert.AreEqual(deliverTime.ToString(), doNotDeliverBefore.At.ToString());

            Assert.True(fakeBatchPipeline.TransportOperations.First().DeliveryConstraints.TryGet(out DiscardIfNotReceivedBefore discard));
            Assert.AreEqual(maxTime, discard.MaxTime);

            Assert.Null(fakeOutbox.StoredMessage);
        }

        [Test]
        public async Task Should_honor_stored_direct_routing()
        {
            var messageId = "id";
            var options = new Dictionary<string, string>();

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
            var options = new Dictionary<string, string>();

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

        [Test]
        public async Task Should_set_scoped_session()
        {
            var context = CreateContext(fakeBatchPipeline, "id");

            IScopedMessageSession s1 = null, s2 = null;
            await Task.WhenAll(Invoke(context, c =>
            {
                s1 = scopedSessionHolder.Session.Value;
                return Task.CompletedTask;
            }), Invoke(context, c =>
            {
                s2 = scopedSessionHolder.Session.Value;
                return Task.CompletedTask;
            }));

            Assert.NotNull(s1);
            Assert.NotNull(s2);
            Assert.AreNotSame(s1, s2);
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
            scopedSessionHolder = new ScopedSessionHolder();

            behavior = new TransportReceiveToPhysicalMessageProcessingConnector(fakeOutbox, scopedSessionHolder);
        }

        Task Invoke(ITransportReceiveContext context, Func<IIncomingPhysicalMessageContext, Task> next = null)
        {
            next = next ?? (c => TaskEx.CompletedTask);
            return behavior.Invoke(context, next);
        }

        TransportReceiveToPhysicalMessageProcessingConnector behavior;

        FakeBatchPipeline fakeBatchPipeline;
        FakeOutboxStorage fakeOutbox;
        ScopedSessionHolder scopedSessionHolder;

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
                return (IPipeline<TContext>) pipeline;
            }

            IPipeline<IBatchDispatchContext> pipeline;
        }

        class FakeBatchPipeline : IPipeline<IBatchDispatchContext>
        {
            public IEnumerable<TransportOperation> TransportOperations { get; set; }

            public Task Invoke(IBatchDispatchContext context)
            {
                TransportOperations = context.Operations;

                return TaskEx.CompletedTask;
            }
        }
    }
}