namespace NServiceBus.Core.Tests.Timeout
{
    using System;
    using NServiceBus.Timeout.Core;
    using NUnit.Framework;

    [TestFixture]
    public class When_receiving_timeouts
    {
        FakeMessageSender messageSender;
        DefaultTimeoutManager manager;

        [SetUp]
        public void Setup()
        {
            Address.InitializeLocalAddress("MyEndpoint");


            messageSender = new FakeMessageSender();
            manager = new DefaultTimeoutManager
                {
                    MessageSender = messageSender,
                };
        }

        [Test]
        public void Should_dispatch_timeout_if_is_due_now()
        {
            manager.PushTimeout(new TimeoutData
                {
                    Time = DateTime.UtcNow,
                });

            Assert.AreEqual(1, messageSender.MessagesSent);
        }
    }
}