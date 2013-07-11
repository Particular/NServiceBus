namespace NServiceBus.Unicast.Tests
{
    using System;
    using System.Configuration;
    using NServiceBus.Config;
    using NUnit.Framework;

    [TestFixture]
    public class When_no_time_to_be_received_on_forwarded_messages_is_specified:ConfigContext
    {
        [Test]
        public void Should_return_zero_if_not_found()
        {
            Assert.AreEqual(TimeSpan.Zero, GetSection("UnicastBus_with_empty_ttr").TimeToBeReceivedOnForwardedMessages);
        }
    }

    [TestFixture]
    public class When_time_to_be_received_on_forwarded_messages_is_specified : ConfigContext
    {
        [Test]
        public void Should_return_the_configured_value()
        {
            Assert.AreEqual(TimeSpan.FromMinutes(30), GetSection("UnicastBus_with_ttr_set").TimeToBeReceivedOnForwardedMessages);
        }
    }

    public class ConfigContext
    {
        protected UnicastBusConfig GetSection(string name)
        {
            return ConfigurationManager.GetSection(name) as UnicastBusConfig;
        }
    }
}
