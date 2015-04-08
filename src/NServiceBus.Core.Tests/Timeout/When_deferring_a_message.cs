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
            var deferrer = new TimeoutManagerDeferrer
                           {
                               TimeoutManagerAddress = "TimeoutManager"
                           };
            var sender = new FakeMessageSender();
            deferrer.MessageSender = sender;

            var deliverAt = DateTime.Now.AddDays(1);
            var options = new SendMessageOptions("destination", deliverAt);
            deferrer.Defer(new OutgoingMessage("message id",new Dictionary<string, string>(),new byte[0]), options);

            Assert.AreEqual(DateTimeExtensions.ToWireFormattedString(deliverAt), sender.Messages.First().Headers[TimeoutManagerHeaders.Expire]);
        }

        [Test]
        public void Should_set_the_expiry_header_to_a_absolute_utc_time_calculated_based_on_delay()
        {
            var deferrer = new TimeoutManagerDeferrer
            {
                TimeoutManagerAddress = "TimeoutManager"
            };
            var sender = new FakeMessageSender();
            deferrer.MessageSender = sender;

            var delay = TimeSpan.FromDays(1);
            var options = new SendMessageOptions("destination", delayDeliveryFor: delay);
            deferrer.Defer(new OutgoingMessage("message id",new Dictionary<string, string>(),new byte[0]), options);

            var expireAt = DateTimeExtensions.ToUtcDateTime(sender.Messages.First().Headers[TimeoutManagerHeaders.Expire]);
            Assert.IsTrue(expireAt <= DateTime.UtcNow + delay);
        }

        [Test]
        public void Should_use_utc_when_comparing()
        {
            var deferrer = new DispatchMessageToTransportBehavior();
            var sender = new FakeMessageSender();
            deferrer.MessageSender = sender;
            deferrer.HostInfo = new HostInformation(Guid.NewGuid(),"Display name");
            var settings = new SettingsHolder();
            settings.Set("EndpointName", "EndpointName");
            deferrer.Settings = settings;

            deferrer.Invoke(new PhysicalOutgoingContextStageBehavior.Context(null, new OutgoingContext(null, new SendMessageOptions("Destination"),null,new Dictionary<string, string>(),null,MessageIntentEnum.Send)), () => { });

            Assert.AreEqual(1, sender.Messages.Count);
        }

        class FakeMessageSender : ISendMessages
        {

            public List<OutgoingMessage> Messages = new List<OutgoingMessage>();


            public void Send(OutgoingMessage message, TransportSendOptions sendOptions)
            {
                Messages.Add(message);
            }
        }
    }
}