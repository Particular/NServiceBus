namespace NServiceBus.Timeout
{
    using System;
    using Logging;
    using Transports;
    using Unicast;
    using Unicast.Transport;

    class TimeoutManagerDeferrer : IDeferMessages
    {
        public ISendMessages MessageSender { get; set; }
        public string TimeoutManagerAddress { get; set; }
        public Configure Configure { get; set; }

        public void Defer(OutgoingMessage message, SendOptions sendOptions)
        {
            message.Headers[TimeoutManagerHeaders.RouteExpiredTimeoutTo] = sendOptions.Destination;

            DateTime deliverAt;

            if (sendOptions.DelayDeliveryFor.HasValue)
            {
                deliverAt = DateTime.UtcNow + sendOptions.DelayDeliveryFor.Value;
            }
            else
            {
                if (sendOptions.DeliverAt.HasValue)
                {
                    deliverAt = sendOptions.DeliverAt.Value;    
                }
                else
                {
                    throw new ArgumentException("A delivery time needs to be specified for Deferred messages");
                }
                
            }

            message.Headers[TimeoutManagerHeaders.Expire] = DateTimeExtensions.ToWireFormattedString(deliverAt);
            
            try
            {
                MessageSender.Send(message, new TransportSendOptions(TimeoutManagerAddress, enlistInReceiveTransaction: sendOptions.EnlistInReceiveTransaction));
            }
            catch (Exception ex)
            {
                Log.Error("There was a problem deferring the message. Make sure that DisableTimeoutManager was not called for your endpoint.", ex);
                throw;
            }
        }

        public void ClearDeferredMessages(string headerKey, string headerValue)
        {
            var controlMessage = ControlMessageFactory.Create(MessageIntentEnum.Send);

            controlMessage.Headers[headerKey] = headerValue;
            controlMessage.Headers[TimeoutManagerHeaders.ClearTimeouts] = bool.TrueString;

            MessageSender.Send(controlMessage, new TransportSendOptions(TimeoutManagerAddress));
        }

        static ILog Log = LogManager.GetLogger<TimeoutManagerDeferrer>();
    }
}
