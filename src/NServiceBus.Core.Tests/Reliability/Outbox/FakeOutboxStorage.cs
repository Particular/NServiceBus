#nullable enable

namespace NServiceBus.Core.Tests.Reliability.Outbox;

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Extensibility;
using Fakes;
using NServiceBus.Outbox;

class FakeOutboxStorage : IOutboxStorage
{
    public OutboxMessage? ExistingMessage { get; set; }

    public OutboxMessage? StoredMessage { get; set; }

    public bool WasDispatched { get; set; }

    public Task<OutboxMessage?> Get(string messageId, ContextBag options, CancellationToken cancellationToken = default)
    {
        if (ExistingMessage is not null && ExistingMessage.MessageId == messageId)
        {
            return Task.FromResult<OutboxMessage?>(new OutboxMessage(
                ExistingMessage.MessageId,
                [.. ExistingMessage.TransportOperations.Select(CopyOperation)]));
        }

        return Task.FromResult(default(OutboxMessage));
    }

    public Task Store(OutboxMessage message, IOutboxTransaction transaction, ContextBag options, CancellationToken cancellationToken = default)
    {
        StoredMessage = message;
        return Task.CompletedTask;
    }

    public Task SetAsDispatched(string messageId, ContextBag options, CancellationToken cancellationToken = default)
    {
        WasDispatched = true;
        return Task.CompletedTask;
    }

    public Task<IOutboxTransaction> BeginTransaction(ContextBag context, CancellationToken cancellationToken = default) =>
        Task.FromResult<IOutboxTransaction>(new FakeOutboxTransaction());

    static TransportOperation CopyOperation(TransportOperation operation)
    {
        var headers = operation.Headers != null
            ? new Dictionary<string, string>(operation.Headers)
            : [];

        var options = operation.Options != null
            ? new Transport.DispatchProperties(operation.Options)
            : [];

        var body = operation.Body.IsEmpty
            ? []
            : operation.Body.ToArray();

        return new TransportOperation(operation.MessageId, options, body, headers);
    }
}