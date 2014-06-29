namespace NServiceBus.Core.Tests.Transport
{
    using System;
    using System.Transactions;
    using Fakes;
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

            TransportReceiver = new TransportReceiver(new TransactionSettings(true, TimeSpan.FromSeconds(30), IsolationLevel.ReadCommitted, 5, false,false), 1, 0, fakeReceiver, new FakeFailureManager(), new SettingsHolder());

        }

        protected FakeReceiver fakeReceiver;
        protected TransportReceiver TransportReceiver;
    }
}