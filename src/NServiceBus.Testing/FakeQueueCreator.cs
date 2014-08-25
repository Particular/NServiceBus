namespace NServiceBus.Testing
{
    using NServiceBus.Transports;

    class FakeQueueCreator : ICreateQueues
    {
        public void CreateQueueIfNecessary(string address, string account)
        {
            //no-op
        }
    }
}