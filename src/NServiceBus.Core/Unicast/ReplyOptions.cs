namespace NServiceBus.Unicast
{
    using System;

    /// <summary>
    /// Additional options that only apply for reply messages
    /// </summary>
    class ReplyOptions : SendOptions
    {
        /// <summary>
        /// Both a destination and a correlation id is required when replying
        /// </summary>
        /// <param name="destination"></param>
        /// <param name="correlationId"></param>
        public ReplyOptions(Address destination, string correlationId):base(destination)
        {
            if (destination == null)
            {
                throw new InvalidOperationException("Can't reply with null reply-to-address field. It can happen if you are using a SendOnly client. See http://particular.net/articles/one-way-send-only-endpoints");
            }
            CorrelationId = correlationId;
        }
    }
}