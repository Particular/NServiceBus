namespace NServiceBus.Core.Tests.Pipeline.Incoming;

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using NServiceBus.AcceptanceTests.Core.OpenTelemetry.Metrics;
using NServiceBus.Core.Tests.OpenTelemetry;
using NServiceBus.Pipeline;
using NUnit.Framework;
using Serialization;
using Testing;
using Unicast.Messages;

[TestFixture]
public class SerializeMessageConnectorTests
{
    [Test]
    public async Task Should_set_content_type_header()
    {
        var registry = new MessageMetadataRegistry();
        registry.Initialize(new Conventions().IsMessageType, true);

        registry.RegisterMessageTypes(
        [
            typeof(MyMessage)
        ]);

        var context = new TestableOutgoingLogicalMessageContext
        {
            Message = new OutgoingLogicalMessage(typeof(MyMessage), new MyMessage())
        };

        var behavior = new SerializeMessageConnector(new FakeSerializer("myContentType"), registry, new IncomingPipelineMetrics(new TestMeterFactory(), "queue", "disc", new MetersOptions()));

        await behavior.Invoke(context, c => Task.CompletedTask);

        Assert.That(context.Headers[Headers.ContentType], Is.EqualTo("myContentType"));
    }

    [Test]
    public async Task Should_tag_serialize_time_with_queue_and_discriminator_from_the_endpoint_configuration()
    {
        // A send from IMessageSession has no incoming pipeline to inherit tags from, so the values have to come
        // from the endpoint configuration the metrics were constructed with.
        using var metricsListener = TestingMetricListener.SetupNServiceBusMetricsListener();

        await InvokeSerializer(CreateContext(), queueName: "queue", discriminator: "disc");

        using (Assert.EnterMultipleScope())
        {
            Assert.That(metricsListener.AssertTagKeyExists(MessageSerializeTime, "nservicebus.queue"), Is.EqualTo("queue"));
            Assert.That(metricsListener.AssertTagKeyExists(MessageSerializeTime, "nservicebus.discriminator"), Is.EqualTo("disc"));
        }
    }

    [Test]
    public async Task Should_prefer_queue_and_discriminator_from_the_incoming_pipeline_tags()
    {
        // A send from a message handler chains its context to the incoming pipeline, whose tags win over the
        // endpoint configuration defaults.
        using var metricsListener = TestingMetricListener.SetupNServiceBusMetricsListener();

        var context = CreateContext();
        var incomingTags = context.Extensions.GetOrCreate<IncomingPipelineMetricTags>();
        incomingTags.Add("nservicebus.queue", "queue-from-incoming-pipeline");
        incomingTags.Add("nservicebus.discriminator", "disc-from-incoming-pipeline");

        await InvokeSerializer(context, queueName: "queue", discriminator: "disc");

        using (Assert.EnterMultipleScope())
        {
            Assert.That(metricsListener.AssertTagKeyExists(MessageSerializeTime, "nservicebus.queue"), Is.EqualTo("queue-from-incoming-pipeline"));
            Assert.That(metricsListener.AssertTagKeyExists(MessageSerializeTime, "nservicebus.discriminator"), Is.EqualTo("disc-from-incoming-pipeline"));
        }
    }

    [Test]
    public async Task Should_not_tag_serialize_time_with_queue_and_discriminator_when_send_only()
    {
        // A send-only endpoint has no receive queue, so neither tag is available to report.
        using var metricsListener = TestingMetricListener.SetupNServiceBusMetricsListener();

        await InvokeSerializer(CreateContext(), queueName: null, discriminator: null);

        using (Assert.EnterMultipleScope())
        {
            metricsListener.AssertTagKeyDoesNotExist(MessageSerializeTime, "nservicebus.queue");
            metricsListener.AssertTagKeyDoesNotExist(MessageSerializeTime, "nservicebus.discriminator");
            // the metric itself is still recorded, only the two tags are missing
            Assert.That(metricsListener.AssertTagKeyExists(MessageSerializeTime, "nservicebus.message_type"), Is.EqualTo(typeof(MyMessage).FullName));
        }
    }

    static TestableOutgoingLogicalMessageContext CreateContext() =>
        new() { Message = new OutgoingLogicalMessage(typeof(MyMessage), new MyMessage()) };

    static Task InvokeSerializer(TestableOutgoingLogicalMessageContext context, string queueName, string discriminator)
    {
        var registry = new MessageMetadataRegistry();
        registry.Initialize(new Conventions().IsMessageType, true);
        registry.RegisterMessageTypes([typeof(MyMessage)]);

        var behavior = new SerializeMessageConnector(
            new FakeSerializer("myContentType"),
            registry,
            new IncomingPipelineMetrics(new TestMeterFactory(), queueName, discriminator, new MetersOptions()));

        return behavior.Invoke(context, _ => Task.CompletedTask);
    }

    const string MessageSerializeTime = "nservicebus.messaging.serialize_time";

    class FakeSerializer(string contentType) : IMessageSerializer
    {
        public void Serialize(object message, Stream stream)
        {
        }

        public object[] Deserialize(ReadOnlyMemory<byte> body, IList<Type> messageTypes = null) => throw new NotImplementedException();

        public string ContentType { get; } = contentType;
    }

    class MyMessage : IMessage;
}