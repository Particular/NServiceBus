namespace NServiceBus.Core.Tests.Transport
{
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

            TransportReceiver = new TransportReceiver(TransactionSettings.Default, 1, 0,fakeReceiver, new FakeFailureManager())
            {
                Settings = new SettingsHolder()
            };

        }

        protected FakeReceiver fakeReceiver;
        protected TransportReceiver TransportReceiver;
    }
}