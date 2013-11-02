﻿namespace NServiceBus.Unicast.Tests
{
    using System;
    using Contexts;
    using NUnit.Framework;
    using Timeout;

    [TestFixture]
    public class When_deferring_a_message_with_no_timeoutManager_address_specified : using_the_unicastBus
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
    public class When_deferring_a_message_with_a_set_delay : using_the_unicastBus
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
    public class When_deferring_a_message_with_a_absolute_time : using_the_unicastBus
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


    public class DeferredMessage:IMessage
    {
    }
}