namespace NServiceBus.AcceptanceTests.FakeTransport
{
    using NServiceBus.Transports;

    class FakeQueueCreator : ICreateQueues
    {
        public void CreateQueueIfNecessary(string address, string account)
        {

        }
    }
}