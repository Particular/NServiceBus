﻿namespace NServiceBus.Core.Tests.Reliability.Outbox
{
    using System.Threading;
    using System.Threading.Tasks;
    using Extensibility;
    using Fakes;
    using NServiceBus.Outbox;

    class FakeOutboxStorage : IOutboxStorage
    {
        public OutboxMessage ExistingMessage { get; set; }
        public OutboxMessage StoredMessage { get; set; }

        public bool WasDispatched { get; set; }

        public Task<OutboxMessage> Get(string messageId, ContextBag options, CancellationToken cancellationToken = default)
        {
            if (ExistingMessage != null && ExistingMessage.MessageId == messageId)
            {
                return Task.FromResult(ExistingMessage);
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
    }
}
