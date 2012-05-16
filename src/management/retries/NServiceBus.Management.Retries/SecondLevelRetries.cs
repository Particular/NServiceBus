using System;
using NServiceBus.Faults.Forwarder;
using NServiceBus.Management.Retries.Helpers;
using NServiceBus.Satellites;
using NServiceBus.Unicast.Queuing;
using NServiceBus.Unicast.Transport;
using NServiceBus.Logging;

namespace NServiceBus.Management.Retries
{
    public class SecondLevelRetries : ISatellite
    {
        ILog Logger = LogManager.GetLogger("SecondLevelRetries");

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
            Logger.InfoFormat("Send message to error queue, {0}", FaultManager.ErrorQueue);

            message.ReplyToAddress = TransportMessageHelpers.GetReplyToAddress(message);
            MessageSender.Send(message, FaultManager.ErrorQueue);
        }

        void Defer(TimeSpan defer, TransportMessage message)
        {            
            message.ReplyToAddress = TransportMessageHelpers.GetReplyToAddress(message);

            TransportMessageHelpers.SetHeader(message, Headers.Expire, (DateTime.UtcNow + defer).ToWireFormattedString());
            TransportMessageHelpers.SetHeader(message, SecondLevelRetriesHeaders.Retries, (TransportMessageHelpers.GetNumberOfRetries(message) + 1).ToString());

            if (!TransportMessageHelpers.HeaderExists(message, SecondLevelRetriesHeaders.RetriesTimestamp))
            {
                TransportMessageHelpers.SetHeader(message, SecondLevelRetriesHeaders.RetriesTimestamp, DateTime.UtcNow.ToWireFormattedString());
            }            
            
            Logger.InfoFormat("Defer message and send it to {0} using the timeout manager at {1}", message.ReplyToAddress, TimeoutManagerAddress);            

            MessageSender.Send(message, TimeoutManagerAddress);
        }
    }
}