namespace NServiceBus.Core.Tests.Transport
{
    using System;
    using System.Transactions;
    using Fakes;
    using NServiceBus.Faults;
    using NUnit.Framework;
    using Settings;
    using Unicast.Transport;
    using TransactionSettings = Unicast.Transport.TransactionSettings;

    public class for_the_transactional_transport
    {
        [SetUp]
        public void SetUp()
        {
            fakeReceiver = new FakeReceiver();

            TransportReceiver = new TransportReceiver(new TransactionSettings(true, TimeSpan.FromSeconds(30), IsolationLevel.ReadCommitted, 5, false,false), 1, 0, fakeReceiver, new FakeFailureManager(), new SettingsHolder(), new BusConfiguration().BuildConfiguration());

        }

        protected FakeReceiver fakeReceiver;
        protected TransportReceiver TransportReceiver;

        class FakeFailureManager : IManageMessageFailures
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