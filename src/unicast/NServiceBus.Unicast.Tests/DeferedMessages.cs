namespace NServiceBus.Unicast.Tests
{
    using System;
    using Contexts;
    using NUnit.Framework;
    using Rhino.Mocks;
    using Transport;

    [TestFixture]
    public class When_defering_a_message_with_no_timeoutmanager_address_specified : using_the_unicastbus
    {
        [Test]
        public void Should_use_a_convention_to_set_the_address()
        {
            var conventionBasedAddressToTimeoutManager = MasterNodeAddress.SubScope("Timeouts");

            RegisterMessageType<DeferedMessage>();
            bus.Defer(TimeSpan.FromDays(1),new DeferedMessage());

            VerifyThatMessageWasSentTo(conventionBasedAddressToTimeoutManager);
        }
    }

    [TestFixture]
    public class When_defering_a_message_with_a_set_delay : using_the_unicastbus
    {
        [Test]
        public void Should_set_the_expiry_header_to_a_absolute_utc_time()
        {
            RegisterMessageType<DeferedMessage>();
            var delay = TimeSpan.FromDays(1);

            bus.Defer(delay, new DeferedMessage());

            VerifyThatMessageWasSentWithHeaders(h=> DateTime.Parse(h[Headers.Expire]) <= DateTime.UtcNow + delay);
        }
    }

    [TestFixture]
    public class When_defering_a_message_with_a_absoulte_time : using_the_unicastbus
    {
        [Test]
        public void Should_set_the_expiry_header_to_a_absolute_utc_time()
        {
            RegisterMessageType<DeferedMessage>();
            var time = DateTime.Now + TimeSpan.FromDays(1);

            bus.Defer(time, new DeferedMessage());

            VerifyThatMessageWasSentWithHeaders(h => h[Headers.Expire] == time.ToUniversalTime().ToString());
        }
    }


    public class DeferedMessage:IMessage
    {
    }
}