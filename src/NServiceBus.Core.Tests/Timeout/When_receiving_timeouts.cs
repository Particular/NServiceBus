namespace NServiceBus.Core.Tests.Timeout
{
    using System;
    using NServiceBus.Timeout.Core;
    using NUnit.Framework;

    [TestFixture]
    public class When_receiving_timeouts
    {
        
        [Test]
        public void Should_dispatch_timeout_if_is_due_now()
        {
           var  messageSender = new FakeMessageSender();

            var configure = new BusConfiguration().BuildConfiguration();

            configure.localAddress = new Address("sdad", "asda");
            var manager = new DefaultTimeoutManager
            {
                MessageSender = messageSender,
                Configure = configure
            };

            manager.PushTimeout(new TimeoutData
                {
                    Time = DateTime.UtcNow,
                });

            Assert.AreEqual(1, messageSender.MessagesSent);
        }
    }
}