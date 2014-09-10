namespace NServiceBus.Unicast.Tests
{
    using System;
    using Contexts;
    using NUnit.Framework;
    using Timeout;

    [TestFixture]
    class When_deferring_a_message_with_no_timeoutManager_address_specified : using_the_unicastBus
    {
        [Test]
        public void Should_use_a_convention_to_set_the_address()
        {
            var conventionBasedAddressToTimeoutManager = MasterNodeAddress.SubScope("Timeouts");

            RegisterMessageType<DeferredMessage>();
            bus.Defer(TimeSpan.FromDays(1),new DeferredMessage());

            VerifyThatMessageWasSentTo(conventionBasedAddressToTimeoutManager);
        }
    }

    [TestFixture]
    class When_deferring_a_message_when_involving_worker : using_the_unicastBus
    {
        [Test]
        public void Should_use_master_node_address_when_worker_is_enabled()
        {
            settings.SetDefault("Worker.Enabled", true);
            RegisterMessageType<DeferredMessage>();
            bus.Defer(TimeSpan.FromDays(1), new DeferredMessage());
            VerifyThatMessageWasSentWithHeaders(h => h["NServiceBus.Timeout.RouteExpiredTimeoutTo"] == MasterNodeAddress.ToString());
        }

        [Test]
        public void Should_use_local_address_when_worker_is_disabled()
        {
            settings.SetDefault("Worker.Enabled", false);
            RegisterMessageType<DeferredMessage>();
            bus.Defer(TimeSpan.FromDays(1), new DeferredMessage());
            VerifyThatMessageWasSentWithHeaders(h => h["NServiceBus.Timeout.RouteExpiredTimeoutTo"] == configure.LocalAddress.ToString());
        }
    }

    [TestFixture]
    class When_deferring_a_message_with_a_set_delay : using_the_unicastBus
    {
        [Test]
        public void Should_set_the_expiry_header_to_a_absolute_utc_time()
        {
            RegisterMessageType<DeferredMessage>();
            var delay = TimeSpan.FromDays(1);

            bus.Defer(delay, new DeferredMessage());

            VerifyThatMessageWasSentWithHeaders(h=>
                                                    {
                                                        var e = DateTimeExtensions.ToUtcDateTime(h[TimeoutManagerHeaders.Expire]);
                                                        var now = DateTime.UtcNow + delay;
                                                        return e <= now;
                                                    });
        }
    }

    [TestFixture]
    class When_deferring_a_message_with_a_absolute_time : using_the_unicastBus
    {
        [Test]
        public void Should_set_the_expiry_header_to_a_absolute_utc_time()
        {
            RegisterMessageType<DeferredMessage>();
            var time = DateTime.Now + TimeSpan.FromDays(1);

            bus.Defer(time, new DeferredMessage());

            VerifyThatMessageWasSentWithHeaders(h => h[TimeoutManagerHeaders.Expire] == DateTimeExtensions.ToWireFormattedString(time));
        }
    }

    [TestFixture]
    class When_short_cutting_deferred_messages_that_already_has_expired : using_the_unicastBus
    {
        [Test]
        public void Should_use_utc_when_comparing()
        {
            RegisterMessageType<DeferredMessage>();
            var time = DateTime.Now + TimeSpan.FromSeconds(-5);

            bus.Defer(time, new DeferredMessage());

            //no expiry header should be there since this message will be treated as a send local
            VerifyThatMessageWasSentWithHeaders(h => !h.ContainsKey(TimeoutManagerHeaders.Expire));
        }
    }


    public class DeferredMessage:IMessage
    {
    }
}