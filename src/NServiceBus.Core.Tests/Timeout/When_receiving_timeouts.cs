namespace NServiceBus.Core.Tests.Timeout
{
    using System;
    using System.Collections.Generic;
    using NServiceBus.Extensibility;
    using NServiceBus.InMemory.TimeoutPersister;
    using NServiceBus.Timeout.Core;
    using NUnit.Framework;

    [TestFixture]
    public class When_receiving_timeouts
    {
        
        [Test]
        public void Should_dispatch_timeout_if_is_due_now()
        {
            var options = new TimeoutPersistenceOptions(new ContextBag());
           var  messageSender = new FakeMessageSender();

            var manager = new DefaultTimeoutManager(new InMemoryTimeoutPersister(), messageSender);

            manager.PushTimeout(new TimeoutData
                {
                    Time = DateTime.UtcNow,
                    Destination = "local",
                    Headers = new Dictionary<string, string> { {Headers.MessageId,"msg id"}}
                }, options);

            Assert.AreEqual(1, messageSender.MessagesSent);
        }
    }
}