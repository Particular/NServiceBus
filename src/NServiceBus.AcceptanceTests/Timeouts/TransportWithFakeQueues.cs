
namespace NServiceBus.AcceptanceTests.Timeouts
{
    using System;
    using NServiceBus.Transports;
    using NServiceBus.Unicast;

    public class TransportWithFakeQueues : TransportDefinition
    {
        protected override void Configure(BusConfiguration config)
        {
            config.RegisterComponents(c => c.ConfigureComponent<FakeDequeuer>(DependencyLifecycle.SingleInstance));
            config.RegisterComponents(c => c.ConfigureComponent<FakeSender>(DependencyLifecycle.SingleInstance));
            config.RegisterComponents(c => c.ConfigureComponent<FakeQueueCreator>(DependencyLifecycle.SingleInstance));
        }
    }

    class FakeDequeuer : IDequeueMessages
    {

        public void Init(Address address, Unicast.Transport.TransactionSettings transactionSettings, Func<TransportMessage, bool> tryProcessMessage, Action<TransportMessage, Exception> endProcessMessage)
        {
        }

        public void Start(int maximumConcurrencyLevel)
        {
        }

        public void Stop()
        {
        }
    }

    class FakeQueueCreator : ICreateQueues
    {
        public void CreateQueueIfNecessary(Address address, string account)
        {
        }
    }

    class FakeSender : ISendMessages
    {
        public void Send(TransportMessage message, SendOptions sendOptions)
        {
        }
    }
}