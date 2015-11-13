namespace NServiceBus.AcceptanceTests.FakeTransport
{
    using System.Threading.Tasks;
    using NServiceBus.Transports;

    class FakeQueueCreator : ICreateQueues
    {
        public Task CreateQueueIfNecessary(string address, string account)
        {
            return Task.FromResult(0);
        }
    }
}