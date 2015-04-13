namespace NServiceBus.Core.Tests.Timeout
{
    using System;
    using System.Collections.Generic;
    using NServiceBus.Timeout.Core;
    using NUnit.Framework;

    [TestFixture]
    public class When_receiving_timeouts
    {
        
        [Test]
        public void Should_dispatch_timeout_if_is_due_now()
        {
           var  messageSender = new FakeMessageSender();

            var manager = new DefaultTimeoutManager
            {
                MessageSender = messageSender
            };

            manager.PushTimeout(new TimeoutData
                {
                    Time = DateTime.UtcNow,
                    Destination = "local",
                    Headers = new Dictionary<string, string> { {Headers.MessageId,"msg id"}}
                });

            Assert.AreEqual(1, messageSender.MessagesSent);
        }
    }
}