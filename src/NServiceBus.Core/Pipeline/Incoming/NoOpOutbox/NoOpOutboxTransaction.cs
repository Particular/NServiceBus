namespace NServiceBus.Features
{
    using System.Threading.Tasks;
    using NServiceBus.Outbox;

    class NoOpOutboxTransaction : OutboxTransaction
    {
        public void Dispose()
        {
        }

        public Task Commit()
        {
            return TaskEx.CompletedTask;
        }
    }
}