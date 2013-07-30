﻿namespace NServiceBus.Transports.ActiveMQ
{
    using System;
    using System.Globalization;
    using NServiceBus.Unicast.Queuing;
    using NServiceBus.Unicast.Transport;

    public class ActiveMQMessageDefer : IDeferMessages
    {
        public ISendMessages MessageSender { get; set; }

        public void Defer(TransportMessage message, DateTime processAt, Address address)
        {
            message.Headers[ScheduledMessage.AMQ_SCHEDULED_DELAY] = 
                ((int)processAt.Subtract(DateTime.UtcNow).TotalMilliseconds).ToString(CultureInfo.InvariantCulture);

            this.MessageSender.Send(message, address);
        }

        public void Defer(TransportMessage message, TimeSpan delay, Address address)
        {
            Defer(message, DateTime.UtcNow + delay, address);
        }

        public void ClearDeferredMessages(string headerKey, string headerValue)
        {
            var selector = string.Format("{0} = '{1}'", ActiveMqMessageMapper.ConvertMessageHeaderKeyToActiveMQ(headerKey), headerValue);

            var message = ControlMessage.Create(Address.Local);
            message.Headers[ActiveMqSchedulerManagement.ClearScheduledMessagesSelectorHeader] = selector;

            this.MessageSender.Send(message, Address.Local.SubScope(ActiveMqSchedulerManagement.SubScope));
        }
    }
}