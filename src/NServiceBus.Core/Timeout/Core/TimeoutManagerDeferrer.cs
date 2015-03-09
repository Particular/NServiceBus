﻿namespace NServiceBus.Timeout
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
            sendOptions.Headers[TimeoutManagerHeaders.RouteExpiredTimeoutTo] = sendOptions.Destination;

            DateTime deliverAt;

            if (sendOptions.DelayDeliveryWith.HasValue)
            {
                deliverAt = DateTime.UtcNow + sendOptions.DelayDeliveryWith.Value;
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

            sendOptions.Headers[TimeoutManagerHeaders.Expire] = DateTimeExtensions.ToWireFormattedString(deliverAt);
            
            try
            {
                MessageSender.Send(message, new SendOptions(TimeoutManagerAddress){Headers = sendOptions.Headers});
            }
            catch (Exception ex)
            {
                Log.Error("There was a problem deferring the message. Make sure that DisableTimeoutManager was not called for your endpoint.", ex);
                throw;
            }
        }

        public void ClearDeferredMessages(string headerKey, string headerValue)
        {
            var controlMessage = ControlMessage.Create();

            controlMessage.Headers[headerKey] = headerValue;
            controlMessage.Headers[TimeoutManagerHeaders.ClearTimeouts] = Boolean.TrueString;

            MessageSender.Send(new OutgoingMessage(controlMessage.Body), new SendOptions(TimeoutManagerAddress)
            {
                ReplyToAddress = Configure.PublicReturnAddress,
                Headers = controlMessage.Headers
            });
        }

        static ILog Log = LogManager.GetLogger<TimeoutManagerDeferrer>();
    }
}