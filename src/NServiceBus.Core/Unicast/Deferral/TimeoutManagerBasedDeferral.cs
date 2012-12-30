namespace NServiceBus.Unicast.Deferral
{
    using System;
    using Logging;
    using Queuing;
    using Transport;

    public class TimeoutManagerBasedDeferral:IDeferMessages
    {
        public ISendMessages MessageSender { get; set; }
        public Address TimeoutManagerAddress { get; set; }


        public void Defer(TransportMessage message, DateTime processAt)
        {
            message.Headers[Headers.Expire] = DateTimeExtensions.ToWireFormattedString(processAt);

            try
            {
                MessageSender.Send(message, TimeoutManagerAddress);
            }
            catch (Exception ex)
            {
                Log.Error("There was a problem deferring the message. Make sure that make sure DisableTimeoutManager was not called for your endpoint.", ex);
                throw;
            }
        }

        public void ClearDeferedMessages(string headerKey, string headerValue)
        {
            var controlMessage = ControlMessage.Create(Address.Local);

            controlMessage.Headers[headerKey] = headerValue;
            controlMessage.Headers[Headers.ClearTimeouts] = true.ToString();

            MessageSender.Send(controlMessage, TimeoutManagerAddress);
        }

        private readonly static ILog Log = LogManager.GetLogger("Deferral");
    }
}