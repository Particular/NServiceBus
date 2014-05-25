namespace NServiceBus.Core.Tests.Transport
{
    using Fakes;
    using NUnit.Framework;
    using Unicast.Transport;

    public class for_the_transactional_transport
    {
        [SetUp]
        public void SetUp()
        {
            fakeReceiver = new FakeReceiver();
            var configure = Configure.With();
            TransportReceiver = new TransportReceiver(TransactionSettings.Default, 1, 0, fakeReceiver, new FakeFailureManager(), configure);

        }

        protected FakeReceiver fakeReceiver;
        protected TransportReceiver TransportReceiver;
    }
}