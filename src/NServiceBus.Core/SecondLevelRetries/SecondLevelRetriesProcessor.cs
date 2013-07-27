namespace NServiceBus.SecondLevelRetries
{
    using System;
    using Helpers;
    using Faults.Forwarder;
    using Logging;
    using NServiceBus.Support;
    using Satellites;
    using Transports;
    using TransportMessageHelpers = Helpers.TransportMessageHelpers;

    public class SecondLevelRetriesProcessor : ISatellite
    {
        public ISendMessages MessageSender { get; set; }
        public IDeferMessages MessageDeferrer { get; set; }  
        
        public Address InputAddress { get; set; }

        public bool Disabled { get; set; }

        public FaultManager FaultManager { get; set; }

        public Func<TransportMessage, TimeSpan> RetryPolicy { get; set; }

        public void Start()
        {            
        }

        public void Stop()
        {            
        }
                       
        public bool Handle(TransportMessage message)
        {
            if (Disabled)
            {
                Logger.DebugFormat("The SecondLevelRetries satellite is invoked, but disabled. Sending message to error queue. Make sure that this behavior is expected!");
                SendToErrorQueue(message);
                return true;
            }

            var defer = RetryPolicy.Invoke(message);

            if (defer < TimeSpan.Zero)
            {
                SendToErrorQueue(message);
                return true;
            }

            Defer(defer, message);

            return true;
        }

        void SendToErrorQueue(TransportMessage message)
        {
            Logger.ErrorFormat("SLR has failed to resolve the issue with message {0} and will be forwarded to the error queue at {1}", message.Id, FaultManager.ErrorQueue);

            message.Headers.Remove(Headers.Retries);

            MessageSender.Send(message, FaultManager.ErrorQueue);
        }

        void Defer(TimeSpan defer, TransportMessage message)
        {
            var retryMessageAt = SystemClock.TechnicalTime + defer;

            TransportMessageHelpers.SetHeader(message, Headers.Retries, (TransportMessageHelpers.GetNumberOfRetries(message) + 1).ToString());

            var addressOfFaultingEndpoint = TransportMessageHelpers.GetAddressOfFaultingEndpoint(message);

            if (!TransportMessageHelpers.HeaderExists(message, SecondLevelRetriesHeaders.RetriesTimestamp))
            {
                TransportMessageHelpers.SetHeader(message, SecondLevelRetriesHeaders.RetriesTimestamp, DateTimeExtensions.ToWireFormattedString(SystemClock.TechnicalTime));
            }

            Logger.DebugFormat("Defer message and send it to {0}", addressOfFaultingEndpoint);

            MessageDeferrer.Defer(message, retryMessageAt, addressOfFaultingEndpoint);
        }

        readonly ILog Logger = LogManager.GetLogger(typeof(SecondLevelRetriesProcessor));
    }
}
