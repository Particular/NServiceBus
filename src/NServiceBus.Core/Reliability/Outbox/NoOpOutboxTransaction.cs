namespace NServiceBus
{
    using System.Threading.Tasks;
    using Janitor;
    using Outbox;

    [SkipWeaving]
    sealed class NoOpOutboxTransaction : OutboxTransaction
    {
        public void Dispose()
        {
        }

        public Task Commit() => TaskEx.CompletedTask;
    }
}