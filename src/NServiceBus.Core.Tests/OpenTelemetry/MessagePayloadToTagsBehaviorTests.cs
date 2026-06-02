#nullable enable

namespace NServiceBus.Core.Tests.OpenTelemetry;

using System;
using System.Collections.Immutable;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Helpers;
using NServiceBus.Pipeline;
using NUnit.Framework;
using Testing;
using Unicast.Messages;

[TestFixture]
public class MessagePayloadToTagsBehaviorTests
{
    TestingActivityListener activityListener;

    [SetUp]
    public void SetUp() => activityListener = TestingActivityListener.SetupNServiceBusDiagnosticListener();

    [TearDown]
    public void TearDown() => activityListener.Dispose();

    [Test]
    public async Task Incoming_should_set_base64_encoded_json_body_tag()
    {
        using var activity = ActivitySources.Main.StartActivity("test");

        var message = new TestMessage { Name = "Hello", Value = 42 };
        var context = new TestableIncomingLogicalMessageContext
        {
            Message = new LogicalMessage(new MessageMetadata(typeof(TestMessage)), message)
        };

        await new IncomingMessagePayloadToTagsBehavior().Invoke(context, _ => Task.CompletedTask);

        var tags = activity!.Tags.ToImmutableDictionary();
        Assert.That(tags.ContainsKey("nservicebus.message.body"), Is.True);

        var decoded = Encoding.UTF8.GetString(Convert.FromBase64String(tags["nservicebus.message.body"]!));
        var deserialized = JsonSerializer.Deserialize<TestMessage>(decoded);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(deserialized!.Name, Is.EqualTo("Hello"));
            Assert.That(deserialized.Value, Is.EqualTo(42));
        }
    }

    [Test]
    public async Task Incoming_should_not_set_tag_when_no_active_activity()
    {
        var context = new TestableIncomingLogicalMessageContext
        {
            Message = new LogicalMessage(new MessageMetadata(typeof(TestMessage)), new TestMessage())
        };

        Assert.DoesNotThrowAsync(() => new IncomingMessagePayloadToTagsBehavior().Invoke(context, _ => Task.CompletedTask));
    }

    [Test]
    public async Task Outgoing_should_set_base64_encoded_json_body_tag()
    {
        using var activity = ActivitySources.Main.StartActivity("test");

        var message = new TestMessage { Name = "World", Value = 99 };
        var context = new TestableOutgoingLogicalMessageContext();
        context.UpdateMessage(message);

        await new OutgoingMessagePayloadToTagsBehavior().Invoke(context, _ => Task.CompletedTask);

        var tags = activity!.Tags.ToImmutableDictionary();
        Assert.That(tags.ContainsKey("nservicebus.message.body"), Is.True);

        var decoded = Encoding.UTF8.GetString(Convert.FromBase64String(tags["nservicebus.message.body"]!));
        var deserialized = JsonSerializer.Deserialize<TestMessage>(decoded);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(deserialized!.Name, Is.EqualTo("World"));
            Assert.That(deserialized.Value, Is.EqualTo(99));
        }
    }

    [Test]
    public async Task Outgoing_should_not_set_tag_when_no_active_activity()
    {
        var context = new TestableOutgoingLogicalMessageContext();
        context.UpdateMessage(new TestMessage());

        Assert.DoesNotThrowAsync(() => new OutgoingMessagePayloadToTagsBehavior().Invoke(context, _ => Task.CompletedTask));
    }

    class TestMessage
    {
        public string? Name { get; set; }
        public int Value { get; set; }
    }
}
