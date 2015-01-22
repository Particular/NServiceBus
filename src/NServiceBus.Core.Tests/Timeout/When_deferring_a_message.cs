namespace NServiceBus.Core.Tests.Timeout
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using NServiceBus.Hosting;
    using NServiceBus.Pipeline.Contexts;
    using NServiceBus.Settings;
    using NServiceBus.Timeout;
    using NServiceBus.Transports;
    using NServiceBus.Unicast;
    using NUnit.Framework;

    public class When_deferring_a_message
    {
        [Test]
        public void Should_set_the_expiry_header_to_a_absolute_utc_time()
        {
            var sut = new TimeoutManagerDeferrer();
            var sender = new FakeMessageSender();
            sut.MessageSender = sender;

            var options = new SendOptions("destination");
            var deliverAt = DateTime.Now.AddDays(1);
            options.DeliverAt = deliverAt;
            sut.Defer(new TransportMessage(), options);

            Assert.AreEqual(DateTimeExtensions.ToWireFormattedString(deliverAt), sender.Messages.First().Headers[TimeoutManagerHeaders.Expire]);
        }

        [Test]
        public void Should_set_the_expiry_header_to_a_absolute_utc_time_calculated_based_on_delay()
        {
            var sut = new TimeoutManagerDeferrer();
            var sender = new FakeMessageSender();
            sut.MessageSender = sender;

            var options = new SendOptions("destination");
            var delay = TimeSpan.FromDays(1);
            options.DelayDeliveryWith = delay;
            sut.Defer(new TransportMessage(), options);

            var expireAt = DateTimeExtensions.ToUtcDateTime(sender.Messages.First().Headers[TimeoutManagerHeaders.Expire]);
            Assert.IsTrue(expireAt < DateTime.UtcNow + delay);
        }

        [Test]
        public void Should_use_utc_when_comparing()
        {
            var sut = new DispatchMessageToTransportBehavior();
            var sender = new FakeMessageSender();
            sut.MessageSender = sender;
            sut.HostInfo = new HostInformation(Guid.NewGuid(),"Display name");
            var settings = new SettingsHolder();
            settings.Set("EndpointName", "EndpointName");
            sut.Settings = settings;

            sut.Invoke(new PhysicalOutgoingContextStageBehavior.Context(new TransportMessage(), new OutgoingContext(null, new SendOptions("Destination"), null)), () => { });

            Assert.AreEqual(1, sender.Messages.Count);
        }

        private class FakeMessageSender : ISendMessages
        {
            public List<TransportMessage> Messages = new List<TransportMessage>();

            public void Send(TransportMessage message, SendOptions sendOptions)
            {
                Messages.Add(message);
            }
        }
    }
}