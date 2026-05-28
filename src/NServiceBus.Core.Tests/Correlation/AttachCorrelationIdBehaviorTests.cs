namespace NServiceBus.Core.Tests.Correlation;

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NUnit.Framework;
using Testing;
using Transport;

[TestFixture]
public class AttachCorrelationIdBehaviorTests
{
    [Test]
    public async Task Should_use_custom_correlation_id_from_operation_properties_when_present()
    {
        var behavior = new AttachCorrelationIdBehavior();
        var context = new TestableOutgoingLogicalMessageContext();
        var customCorrelationId = "custom-correlation-id";

        context.Extensions.Set(new AttachCorrelationIdBehavior.State
        {
            CustomCorrelationId = customCorrelationId
        });

        await behavior.Invoke(context, _ => Task.CompletedTask);

        Assert.That(context.Headers[Headers.CorrelationId], Is.EqualTo(customCorrelationId));
    }

    [Test]
    public async Task Should_use_incoming_correlation_id_when_no_custom_id_exists()
    {
        var behavior = new AttachCorrelationIdBehavior();
        var context = new TestableOutgoingLogicalMessageContext();

        context.Extensions.Set(new IncomingMessage("incoming-id", new Dictionary<string, string>
        {
            { Headers.CorrelationId, "incoming-correlation-id" }
        }, Array.Empty<byte>()));

        await behavior.Invoke(context, _ => Task.CompletedTask);

        Assert.That(context.Headers[Headers.CorrelationId], Is.EqualTo("incoming-correlation-id"));
    }

    [Test]
    public async Task Should_fall_back_to_incoming_message_id_when_incoming_correlation_id_is_missing_or_empty()
    {
        var behavior = new AttachCorrelationIdBehavior();
        var context = new TestableOutgoingLogicalMessageContext();

        context.Extensions.Set(new IncomingMessage("incoming-id", new Dictionary<string, string>
        {
            { Headers.CorrelationId, string.Empty },
            { Headers.MessageId, "incoming-header-message-id" }
        }, Array.Empty<byte>()));

        await behavior.Invoke(context, _ => Task.CompletedTask);

        Assert.That(context.Headers[Headers.CorrelationId], Is.EqualTo("incoming-header-message-id"));
    }

    [Test]
    public async Task Should_fall_back_to_outgoing_context_message_id_when_no_custom_or_incoming_values_exist()
    {
        var behavior = new AttachCorrelationIdBehavior();
        var context = new TestableOutgoingLogicalMessageContext();

        await behavior.Invoke(context, _ => Task.CompletedTask);

        Assert.That(context.Headers[Headers.CorrelationId], Is.EqualTo(context.MessageId));
    }

    [Test]
    public async Task Should_overwrite_existing_outgoing_correlation_id_with_selected_value()
    {
        var behavior = new AttachCorrelationIdBehavior();
        var context = new TestableOutgoingLogicalMessageContext();

        context.Headers[Headers.CorrelationId] = "existing-value";
        context.Extensions.Set(new AttachCorrelationIdBehavior.State
        {
            CustomCorrelationId = "custom-correlation-id"
        });

        await behavior.Invoke(context, _ => Task.CompletedTask);

        Assert.That(context.Headers[Headers.CorrelationId], Is.EqualTo("custom-correlation-id"));
    }
}
