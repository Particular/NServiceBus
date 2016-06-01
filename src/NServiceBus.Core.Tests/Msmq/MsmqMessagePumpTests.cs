namespace NServiceBus.Core.Tests.Msmq
{
    using System;
    using NServiceBus.Transports;
    using NUnit.Framework;

    [TestFixture]
    public class MsmqMessagePumpTests
    {

        [Test]
        public void Should_throw_an_exception()
        {
            var messagePump = new MessagePump(mode => null);
            var pushSettings = new PushSettings("queue@remote", "error", false, TransportTransactionMode.None);

            var exception = Assert.Throws<Exception>(() =>
            {
                messagePump.Init(context => null, null, pushSettings);
            });

            Assert.That(exception.Message, Does.Contain($"MSMQ Dequeuing can only run against the local machine. Invalid inputQueue name '{pushSettings.InputQueue}'."));
        }
    }
}