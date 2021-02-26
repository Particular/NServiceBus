namespace NServiceBus
{
    using System.Threading;
    using System.Threading.Tasks;
    using Outbox;

    class NoOpOutboxTransaction : OutboxTransaction
    {
        public void Dispose()
        {
        }

        public Task Commit(CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }
    }
}