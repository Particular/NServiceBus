namespace NServiceBus
{
    using System.Threading;
    using System.Threading.Tasks;
    using Janitor;
    using Outbox;

    [SkipWeaving]
    sealed class NoOpOutboxTransaction : IOutboxTransaction
    {
        public void Dispose()
        {
        }

        public Task Commit(CancellationToken cancellationToken = default) => Task.CompletedTask;
    }
}