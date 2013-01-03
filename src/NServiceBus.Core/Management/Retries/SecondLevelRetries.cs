namespace NServiceBus.Management.Retries
{
    using System;
    using Faults.Forwarder;
    using Logging;
    using Satellites;
    using Unicast.Queuing;
    using TransportMessageHelpers = Helpers.TransportMessageHelpers;

    public class SecondLevelRetries : ISatellite
    {
        readonly ILog Logger = LogManager.GetLogger("SecondLevelRetries");

        public ISendMessages MessageSender { get; set; }        
        
        public Address InputAddress { get; set; }

        public Address TimeoutManagerAddress { get; set; }

        public bool Disabled { get; set; }

        public FaultManager FaultManager { get; set; }

        public static Func<TransportMessage, TimeSpan> RetryPolicy = DefaultRetryPolicy.Validate;

        public static Func<TransportMessage, bool> TimeoutPolicy = DefaultRetryPolicy.HasTimedOut;

        public void Start()
        {            
        }

        public void Stop()
        {            
        }
                       
        public void Handle(TransportMessage message)
        {
            if (Disabled)
            {
                Logger.DebugFormat("The SecondLevelRetries satellite is invoked, but disabled. Sending message to error queue. Make sure that this behaviour is expected!");
                SendToErrorQueue(message);
                return;
            }

            var defer = RetryPolicy.Invoke(message);
            var hasTimedOut = TimeoutPolicy.Invoke(message);

            if (defer < TimeSpan.Zero || hasTimedOut)
            {
                SendToErrorQueue(message);
                return;
            }

            Defer(defer, message); 
        }

        void SendToErrorQueue(TransportMessage message)
        {
            Logger.ErrorFormat("SLR has failed to resolve the issue with message {0} and will be forwarded to the error queue at {1}", message.IdForCorrelation, FaultManager.ErrorQueue);

            MessageSender.Send(message, FaultManager.ErrorQueue);
        }

        void Defer(TimeSpan defer, TransportMessage message)
        {
            TransportMessageHelpers.SetHeader(message, Headers.Expire, DateTimeExtensions.ToWireFormattedString(DateTime.UtcNow + defer));
            TransportMessageHelpers.SetHeader(message, Headers.Retries, (TransportMessageHelpers.GetNumberOfRetries(message) + 1).ToString());

            var faultingEndpointAddress = TransportMessageHelpers.GetAddressOfFaultingEndpoint(message);

            //tell the TM to route this message to the endpoint where this message failed
            TransportMessageHelpers.SetHeader(message, Headers.RouteExpiredTimeoutTo, faultingEndpointAddress.ToString());

            if (!TransportMessageHelpers.HeaderExists(message, SecondLevelRetriesHeaders.RetriesTimestamp))
            {
                TransportMessageHelpers.SetHeader(message, SecondLevelRetriesHeaders.RetriesTimestamp, DateTimeExtensions.ToWireFormattedString(DateTime.UtcNow));
            }

            Logger.DebugFormat("Defer message and send it to {0} using the timeout manager at {1}", faultingEndpointAddress, TimeoutManagerAddress);            

            MessageSender.Send(message, TimeoutManagerAddress);
        }
    }
}
