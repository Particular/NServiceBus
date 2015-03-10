namespace NServiceBus
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Allows the users to control how the send is performed
    /// </summary>
    public class SendContext
    {
        /// <summary>
        /// Set a header for the message to be send
        /// </summary>
        /// <param name="key">The header key</param>
        /// <param name="value">The header value</param>
        public void SetHeader(string key, string value)
        {
            Headers[key] = value;
        }

     
        /// <summary>
        /// Tells the bus to wait the specified amount of time before delivering the message
        /// </summary>
        /// <param name="within"></param>
        public void DelayDeliveryWith(TimeSpan within)
        {

            Delay = within;
            At = null;
        }

        internal TimeSpan? Delay;
        internal DateTime? At;
        internal string Destination;
        internal bool IsReply;
        internal string CorrelationId;
        internal Dictionary<string, string> Headers = new Dictionary<string, string>();

        /// <summary>
        /// Set a specific destination for the message
        /// </summary>
        /// <param name="destination"></param>
        public void SetDestination(string destination)
        {
            Destination = destination;
        }

        /// <summary>
        /// Specified a custom currealtion id for the message
        /// </summary>
        /// <param name="correlationId"></param>
        public void SetCorrelationId(string correlationId)
        {
            CorrelationId = correlationId;
        }


        /// <summary>
        /// Tells the bus to deliver the message at the given time
        /// </summary>
        /// <param name="at"></param>
        public void DeliverAt(DateTime at)
        {
            At = at;
            Delay = null;
        }
    }
}