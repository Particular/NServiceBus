namespace NServiceBus.Timeout
{
    using System;
    using Logging;
    using Transports;
    using Unicast.Transport;

    public class TimeoutManagerDeferrer : IDeferMessages
    {
        public ISendMessages MessageSender { get; set; }
        public Address TimeoutManagerAddress { get; set; }


        public void Defer(TransportMessage message, DateTime processAt, Address address)
        {
            message.Headers[TimeoutManagerHeaders.Expire] = DateTimeExtensions.ToWireFormattedString(processAt);

            message.Headers[TimeoutManagerHeaders.RouteExpiredTimeoutTo] = address.ToString();

            try
            {
                MessageSender.Send(message, TimeoutManagerAddress);
            }
            catch (Exception ex)
            {
                Log.Error(
                    "There was a problem deferring the message. Make sure that make sure DisableTimeoutManager was not called for your endpoint.",
                    ex);
                throw;
            }
        }

        public void ClearDeferredMessages(string headerKey, string headerValue)
        {
            var controlMessage = ControlMessage.Create(Address.Local);

            controlMessage.Headers[headerKey] = headerValue;
            controlMessage.Headers[TimeoutManagerHeaders.ClearTimeouts] = Boolean.TrueString;

            MessageSender.Send(controlMessage, TimeoutManagerAddress);
        }

        static readonly ILog Log = LogManager.GetLogger(typeof (TimeoutManagerDeferrer));
    }
}