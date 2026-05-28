namespace NServiceBus.Core.Tests.Sagas;

using System;
using System.Threading.Tasks;
using NUnit.Framework;
using Testing;
using Transport;

[TestFixture]
public class PopulateAutoCorrelationHeadersForRepliesBehaviorTests
{
    [Test]
    public async Task Should_copy_originating_saga_id_to_outgoing_saga_id()
    {
        var context = CreateContextWithIncomingMessage("saga-1", "saga-type");

        await new PopulateAutoCorrelationHeadersForRepliesBehavior().Invoke(context, _ => Task.CompletedTask);

        Assert.That(context.Headers[Headers.SagaId], Is.EqualTo("saga-1"));
    }

    [Test]
    public async Task Should_copy_originating_saga_type_to_outgoing_saga_type()
    {
        var context = CreateContextWithIncomingMessage("saga-1", "saga-type");

        await new PopulateAutoCorrelationHeadersForRepliesBehavior().Invoke(context, _ => Task.CompletedTask);

        Assert.That(context.Headers[Headers.SagaType], Is.EqualTo("saga-type"));
    }

    [Test]
    public async Task Should_not_set_saga_headers_when_no_incoming_physical_message_exists()
    {
        var context = new TestableOutgoingReplyContext();

        await new PopulateAutoCorrelationHeadersForRepliesBehavior().Invoke(context, _ => Task.CompletedTask);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(context.Headers.ContainsKey(Headers.SagaId), Is.False);
            Assert.That(context.Headers.ContainsKey(Headers.SagaType), Is.False);
        }
    }

    [Test]
    public async Task Should_not_set_saga_headers_when_incoming_headers_missing_or_empty()
    {
        var context = CreateContextWithIncomingMessage(string.Empty, null);

        await new PopulateAutoCorrelationHeadersForRepliesBehavior().Invoke(context, _ => Task.CompletedTask);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(context.Headers.ContainsKey(Headers.SagaId), Is.False);
            Assert.That(context.Headers.ContainsKey(Headers.SagaType), Is.False);
        }
    }

    [Test]
    public async Task Should_use_operation_state_values_instead_of_incoming_headers_when_state_exists()
    {
        var context = CreateContextWithIncomingMessage("incoming-id", "incoming-type");
        context.Extensions.Set(new PopulateAutoCorrelationHeadersForRepliesBehavior.State
        {
            SagaIdToUse = "state-id",
            SagaTypeToUse = "state-type"
        });

        await new PopulateAutoCorrelationHeadersForRepliesBehavior().Invoke(context, _ => Task.CompletedTask);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(context.Headers[Headers.SagaId], Is.EqualTo("state-id"));
            Assert.That(context.Headers[Headers.SagaType], Is.EqualTo("state-type"));
        }
    }

    [Test]
    public async Task Should_allow_null_or_empty_state_values_to_suppress_outgoing_saga_headers()
    {
        var context = CreateContextWithIncomingMessage("incoming-id", "incoming-type");
        context.Extensions.Set(new PopulateAutoCorrelationHeadersForRepliesBehavior.State
        {
            SagaIdToUse = string.Empty,
            SagaTypeToUse = null
        });

        await new PopulateAutoCorrelationHeadersForRepliesBehavior().Invoke(context, _ => Task.CompletedTask);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(context.Headers.ContainsKey(Headers.SagaId), Is.False);
            Assert.That(context.Headers.ContainsKey(Headers.SagaType), Is.False);
        }
    }

    static TestableOutgoingReplyContext CreateContextWithIncomingMessage(string sagaId, string sagaType)
    {
        var context = new TestableOutgoingReplyContext();
        var incoming = new IncomingMessage(Guid.NewGuid().ToString(), [], Array.Empty<byte>());

        if (sagaId != null)
        {
            incoming.Headers[Headers.OriginatingSagaId] = sagaId;
        }

        if (sagaType != null)
        {
            incoming.Headers[Headers.OriginatingSagaType] = sagaType;
        }

        context.Extensions.Set(incoming);
        return context;
    }
}
