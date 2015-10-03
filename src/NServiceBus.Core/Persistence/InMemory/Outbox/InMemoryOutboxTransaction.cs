namespace NServiceBus.InMemory.Outbox
{
    using System.Threading.Tasks;
    using NServiceBus.Outbox;

    class InMemoryOutboxTransaction : OutboxTransaction
    {
        public void Dispose()
        {        
        }

        public Task Commit()
        {
            return TaskEx.Completed;
        }
    }
}