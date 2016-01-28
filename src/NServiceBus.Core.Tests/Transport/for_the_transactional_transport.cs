namespace NServiceBus.Core.Tests.Transport
{
    using System;
    using Fakes;
    using NServiceBus.Faults;
    using NUnit.Framework;
    using Unicast.Transport;

    public class for_the_transactional_transport
    {
        [SetUp]
        public void SetUp()
        {
            fakeReceiver = new FakeReceiver();

            TransportReceiver = new TransportReceiver
                {
                    FailureManager = new FakeFailureManager(),
                    Receiver = fakeReceiver,
                    TransactionSettings = TransactionSettings.Default
                };

        }

        protected FakeReceiver fakeReceiver;
        protected TransportReceiver TransportReceiver;

        public class FakeFailureManager : IManageMessageFailures
        {
            public void SerializationFailedForMessage(TransportMessage message, Exception e)
            {

            }

            public void ProcessingAlwaysFailsForMessage(TransportMessage message, Exception e)
            {

            }

            public void Init(Address address)
            {

            }
        }
    }
}