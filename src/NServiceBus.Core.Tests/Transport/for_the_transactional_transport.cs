namespace NServiceBus.Core.Tests.Transport
{
    using Fakes;
    using NUnit.Framework;
    using Unicast.Transport.Transactional;

    public class for_the_transactional_transport
    {
        [SetUp]
        public void SetUp()
        {
            fakeReceiver = new FakeReceiver();

            transport = new TransactionalTransport
                {
                    FailureManager = new FakeFailureManager(),
                    Receiver = fakeReceiver,
                };

        }

        protected FakeReceiver fakeReceiver;
        protected TransactionalTransport transport;
    }
}