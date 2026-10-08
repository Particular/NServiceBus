#nullable enable

namespace NServiceBus.Core.Tests.Reliability.Outbox;

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using AcceptanceTests.Core.OpenTelemetry.Metrics;
using Microsoft.Extensions.Logging.Abstractions;
using NServiceBus.Outbox;
using NServiceBus.Pipeline;
using NServiceBus.Routing;
using NUnit.Framework;
using OpenTelemetry;
using Testing;
using Transport;
using TransportOperation = Transport.TransportOperation;

[TestFixture]
public partial class TransportReceiveToPhysicalMessageConnectorTests
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

        var operationProperties = new DispatchProperties(fakeBatchPipeline.TransportOperations?.First().Properties ?? []);
        var delayDeliveryWith = operationProperties.DelayDeliveryWith;
        Assert.That(delayDeliveryWith, Is.Not.Null);
        Assert.That(delayDeliveryWith.Delay, Is.EqualTo(TimeSpan.FromSeconds(10)));

        var doNotDeliverBefore = operationProperties.DoNotDeliverBefore;
        Assert.That(doNotDeliverBefore, Is.Not.Null);
        Assert.That(doNotDeliverBefore.At.ToString(), Is.EqualTo(deliverTime.ToString()));

        var discard = operationProperties.DiscardIfNotReceivedBefore;
        Assert.That(discard, Is.Not.Null);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(discard.MaxTime, Is.EqualTo(maxTime));

            Assert.That(fakeOutbox.StoredMessage, Is.Null);
        }
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

        var routing = fakeBatchPipeline.TransportOperations?.First().AddressTag as UnicastAddressTag;
        Assert.That(routing, Is.Not.Null);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(routing.Destination, Is.EqualTo("myEndpoint"));
            Assert.That(fakeOutbox.StoredMessage, Is.Null);
        }
    }


    [Test]
    public async Task Should_honor_stored_pubsub_routing()
    {
        var messageId = "id";
        var properties = new DispatchProperties
        {
            ["EventType"] = typeof(MyEvent).AssemblyQualifiedName!
        };

        fakeOutbox.ExistingMessage = new OutboxMessage(messageId, [
            new NServiceBus.Outbox.TransportOperation("x", properties, Array.Empty<byte>(), [])
        ]);

        var context = CreateContext(fakeBatchPipeline, messageId);

        await Invoke(context);

        var routing = fakeBatchPipeline.TransportOperations?.First().AddressTag as MulticastAddressTag;
        Assert.That(routing, Is.Not.Null);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(routing.MessageType, Is.EqualTo(typeof(MyEvent)));
            Assert.That(fakeOutbox.StoredMessage, Is.Null);
        }
    }

    // In v11 the snake_case name is the only one: delete Should_add_outbox_span_tag_when_deduplicating in
    // obsoletes-v10.cs and the attribute on this one.
    [Test]
    [OpenTelemetryV11Defaults]
    public async Task Should_add_snake_case_outbox_span_tag_when_deduplicating()
    {
        string messageId = Guid.NewGuid().ToString();
        fakeOutbox.ExistingMessage = new OutboxMessage(messageId, Array.Empty<NServiceBus.Outbox.TransportOperation>());
        var context = CreateContext(fakeBatchPipeline, messageId);

        using var pipelineActivity = new Activity("test activity");
        pipelineActivity.Start();
        context.Extensions.SetIncomingPipelineActivity(pipelineActivity);

        await Invoke(context);

        Assert.That(pipelineActivity.TagObjects.ToImmutableDictionary()["nservicebus.outbox.deduplicated_message"], Is.EqualTo(true));
    }

    [Test]
    public async Task Should_report_deduplicated_message_metric_when_deduplicating()
    {
        using var metricsListener = TestingMetricListener.SetupNServiceBusMetricsListener();

        string messageId = Guid.NewGuid().ToString();
        fakeOutbox.ExistingMessage = new OutboxMessage(messageId, Array.Empty<NServiceBus.Outbox.TransportOperation>());
        var context = CreateContext(fakeBatchPipeline, messageId);

        await Invoke(context);

        metricsListener.AssertMetric("nservicebus.outbox.duplicates", 1);
    }

    [Test]
    public async Task Should_not_add_batch_dispatch_events_when_no_batched_messages()
    {
        var context = CreateContext(fakeBatchPipeline, Guid.NewGuid().ToString());

        using var pipelineActivity = new Activity("test activity");
        pipelineActivity.Start();
        context.Extensions.SetIncomingPipelineActivity(pipelineActivity);

        await Invoke(context);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(pipelineActivity.Events.Count(e => e.Name == "Start dispatching"), Is.EqualTo(0));
            Assert.That(pipelineActivity.Events.Count(e => e.Name == "Finished dispatching"), Is.EqualTo(0));
        }
    }

    [Test]
    public async Task Should_store_outbox_message_when_outbox_is_enabled()
    {
        var context = CreateContext(fakeBatchPipeline, "id");

        await Invoke(context, c =>
        {
            c.Extensions.Get<PendingTransportOperations>().Add(
                new TransportOperation(new OutgoingMessage("out-1", [], Array.Empty<byte>()), new UnicastAddressTag("destination")));
            return Task.CompletedTask;
        });

        Assert.That(fakeOutbox.StoredMessage, Is.Not.Null);
        var stored = fakeOutbox.StoredMessage!;
        using (Assert.EnterMultipleScope())
        {
            Assert.That(stored.MessageId, Is.EqualTo("id"));
            Assert.That(stored.TransportOperations, Has.Length.EqualTo(1));
            Assert.That(stored.TransportOperations[0].Options!["Destination"], Is.EqualTo("destination"));
        }
    }

    [Test]
    public async Task Should_still_dispatch_when_outbox_is_disabled()
    {
        var noOpBehavior = new TransportReceiveToPhysicalMessageConnector(
            new NoOpOutboxStorage(), new PipelineMetrics(fakeMeterFactory, "queue", "disc"), NullLogger<TransportReceiveToPhysicalMessageConnector>.Instance);

        var context = CreateContext(fakeBatchPipeline, "id");

        await noOpBehavior.Invoke(context, c =>
        {
            c.Extensions.Get<PendingTransportOperations>().AddRange([
                new TransportOperation(new OutgoingMessage("out-1", [], Array.Empty<byte>()), new UnicastAddressTag("destination")),
                new TransportOperation(new OutgoingMessage("out-2", [], Array.Empty<byte>()), new MulticastAddressTag(typeof(MyEvent)))
            ]);
            return Task.CompletedTask;
        });

        Assert.That(fakeBatchPipeline.TransportOperations, Is.Not.Null);
        var dispatched = fakeBatchPipeline.TransportOperations!.ToArray();
        Assert.That(dispatched, Has.Length.EqualTo(2));

        var unicast = dispatched.Single(o => o.Message.MessageId == "out-1");
        var multicast = dispatched.Single(o => o.Message.MessageId == "out-2");
        using (Assert.EnterMultipleScope())
        {
            Assert.That(((UnicastAddressTag)unicast.AddressTag).Destination, Is.EqualTo("destination"));
            Assert.That(((MulticastAddressTag)multicast.AddressTag).MessageType, Is.EqualTo(typeof(MyEvent)));

            // With no outbox there is nothing to round-trip the address tag through, so the operations are
            // dispatched without the routing strategy being serialized into their dispatch properties.
            Assert.That(unicast.Properties.ContainsKey("Destination"), Is.False);
            Assert.That(multicast.Properties.ContainsKey("EventType"), Is.False);
        }
    }

    static TestableTransportReceiveContext CreateContext(FakeBatchPipeline pipeline, string messageId)
    {
        var context = new TestableTransportReceiveContext
        {
            Message = new IncomingMessage(messageId, [], Array.Empty<byte>())
        };

        context.Extensions.Set<IPipelineCache>(new FakePipelineCache(pipeline));
        context.Extensions.Set(new PipelineMetricTags());

        return context;
    }

    [SetUp]
    public void SetUp()
    {
        fakeOutbox = new FakeOutboxStorage();
        fakeBatchPipeline = new FakeBatchPipeline();
        fakeMeterFactory = new TestMeterFactory();

        behavior = new TransportReceiveToPhysicalMessageConnector(fakeOutbox, new PipelineMetrics(fakeMeterFactory, "queue", "disc"), NullLogger<TransportReceiveToPhysicalMessageConnector>.Instance);
    }

    [TearDown]
    public void TearDown() => fakeMeterFactory.Dispose();

    Task Invoke(ITransportReceiveContext context, Func<IIncomingPhysicalMessageContext, Task>? next = null) => behavior.Invoke(context, next ?? (_ => Task.CompletedTask));

    TransportReceiveToPhysicalMessageConnector behavior;

    FakeBatchPipeline fakeBatchPipeline;
    FakeOutboxStorage fakeOutbox;
    TestMeterFactory fakeMeterFactory;

    class MyEvent;

    class FakePipelineCache(IPipeline<IBatchDispatchContext> pipeline) : IPipelineCache
    {
        public IPipeline<TContext> Pipeline<TContext>()
            where TContext : IBehaviorContext =>
            (IPipeline<TContext>)pipeline;
    }

    class FakeBatchPipeline : IPipeline<IBatchDispatchContext>
    {
        public IEnumerable<TransportOperation>? TransportOperations { get; set; }

        public Task Invoke(IBatchDispatchContext context)
        {
            TransportOperations = context.Operations;

            return Task.CompletedTask;
        }
    }
}