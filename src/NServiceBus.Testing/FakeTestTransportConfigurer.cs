namespace NServiceBus.Testing
{
    using System;
    using Transports;
    using Unicast;
    using Unicast.Transport;

    class FakeTestTransportConfigurer: IConfigureTransport<FakeTestTransport>
    {
        public void Configure(Configure config)
        {
            config.Configurer.ConfigureComponent<FakeQueueCreator>(DependencyLifecycle.InstancePerCall);
            config.Configurer.ConfigureComponent<FakeDequer>(DependencyLifecycle.InstancePerCall);
            config.Configurer.ConfigureComponent<FakeSender>(DependencyLifecycle.InstancePerCall);
        }

        class FakeDequer : IDequeueMessages
        {
            public void Init(Address address, TransactionSettings transactionSettings, Func<TransportMessage, bool> tryProcessMessage, Action<TransportMessage, Exception> endProcessMessage)
            {
                
            }

            public void Start(int maximumConcurrencyLevel)
            {
                
            }

            public void Stop()
            {
                
            }
        }
        class FakeSender:ISendMessages
        {
            public void Send(TransportMessage message, SendOptions sendOptions)
            {
                
            }
        }

        class FakeQueueCreator : ICreateQueues
        {
            public void CreateQueueIfNecessary(Address address, string account)
            {
                //no-op
            }
        }
    }
}