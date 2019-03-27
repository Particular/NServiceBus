namespace NServiceBus.Core.Tests.Msmq
{
    using System;
    using Transport;
    using NUnit.Framework;

    [TestFixture]
    public class MsmqMessagePumpTests
    {

        [Test]
        public void ShouldThrowIfConfiguredToReceiveFromRemoteQueue()
        {
            var messagePump = new MessagePump(mode => null, TimeSpan.Zero);
            var pushSettings = new PushSettings("queue@remote", "error", false, TransportTransactionMode.None);

            var exception = Assert.Throws<Exception>(() =>
            {
                messagePump.Init(context => null, context => null, null, pushSettings);
            });

            Assert.That(exception.Message, Does.Contain($"MSMQ Dequeuing can only run against the local machine. Invalid inputQueue name '{pushSettings.InputQueue}'."));
        }
    }
}