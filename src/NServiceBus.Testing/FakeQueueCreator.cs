namespace NServiceBus.Testing
{
    using NServiceBus.Transports;

    class FakeQueueCreator : ICreateQueues
    {
        public void CreateQueueIfNecessary(Address address, string account)
        {
            //no-op
        }
    }
}