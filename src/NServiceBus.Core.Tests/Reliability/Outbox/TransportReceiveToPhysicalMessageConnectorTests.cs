namespace NServiceBus.Core.Tests.Reliability.Outbox;

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
using OpenTelemetry;
using Settings;
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
            new NServiceBus.Outbox.TransportOperation("x", options, Array.Empty<byte>(), [])
        });

        var context = CreateContext(fakeBatchPipeline, messageId);

        await Invoke(context);

        var operationProperties = new DispatchProperties(fakeBatchPipeline.TransportOperations.First().Properties);
        var delayDeliveryWith = operationProperties.DelayDeliveryWith;
        Assert.NotNull(delayDeliveryWith);
        Assert.That(delayDeliveryWith.Delay, Is.EqualTo(TimeSpan.FromSeconds(10)));

        var doNotDeliverBefore = operationProperties.DoNotDeliverBefore;
        Assert.NotNull(doNotDeliverBefore);
        Assert.That(doNotDeliverBefore.At.ToString(), Is.EqualTo(deliverTime.ToString()));

        var discard = operationProperties.DiscardIfNotReceivedBefore;
        Assert.NotNull(discard);
        Assert.That(discard.MaxTime, Is.EqualTo(maxTime));

        Assert.That(fakeOutbox.StoredMessage, Is.Null);
    }

    [Test]
    public async Task Should_honor_stored_direct_routing()
    {
        var messageId = "id";
        var properties = new DispatchProperties { ["Destination"] = "myEndpoint" };


        fakeOutbox.ExistingMessage = new OutboxMessage(messageId, new[]
        {
            new NServiceBus.Outbox.TransportOperation("x", properties, Array.Empty<byte>(), [])
        });

        var context = CreateContext(fakeBatchPipeline, messageId);

        await Invoke(context);

        var routing = fakeBatchPipeline.TransportOperations.First().AddressTag as UnicastAddressTag;
        Assert.NotNull(routing);
        Assert.That(routing.Destination, Is.EqualTo("myEndpoint"));
        Assert.That(fakeOutbox.StoredMessage, Is.Null);
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
            new NServiceBus.Outbox.TransportOperation("x", properties, Array.Empty<byte>(), [])
        });

        var context = CreateContext(fakeBatchPipeline, messageId);

        await Invoke(context);

        var routing = fakeBatchPipeline.TransportOperations.First().AddressTag as MulticastAddressTag;
        Assert.NotNull(routing);
        Assert.That(routing.MessageType, Is.EqualTo(typeof(MyEvent)));
        Assert.That(fakeOutbox.StoredMessage, Is.Null);
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

        Assert.That(pipelineActivity.TagObjects.ToImmutableDictionary()["nservicebus.outbox.deduplicate-message"], Is.EqualTo(true));
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
        Assert.That(startDispatchErActivityEventsvent.Length, Is.EqualTo(1));
        Assert.That(startDispatchErActivityEventsvent.Single().Tags.ToImmutableDictionary()["message-count"], Is.EqualTo(3));
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

        Assert.That(pipelineActivity.Events.Count(e => e.Name == "Start dispatching"), Is.EqualTo(0));
        Assert.That(pipelineActivity.Events.Count(e => e.Name == "Finished dispatching"), Is.EqualTo(0));
    }

    static TestableTransportReceiveContext CreateContext(FakeBatchPipeline pipeline, string messageId)
    {
        var context = new TestableTransportReceiveContext
        {
            Message = new IncomingMessage(messageId, [], Array.Empty<byte>())
        };

        context.Extensions.Set<IPipelineCache>(new FakePipelineCache(pipeline));

        return context;
    }

    [SetUp]
    public void SetUp()
    {
        fakeOutbox = new FakeOutboxStorage();
        fakeBatchPipeline = new FakeBatchPipeline();

        behavior = new TransportReceiveToPhysicalMessageConnector(fakeOutbox, new IncomingPipelineMetrics(new TestMeterFactory(), "queue", "disc"));
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