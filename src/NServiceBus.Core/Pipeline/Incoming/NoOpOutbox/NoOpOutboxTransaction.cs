namespace NServiceBus
{
    using System.Threading.Tasks;
    using Outbox;

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