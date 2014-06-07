namespace NServiceBus.Timeout.Core
{
    using System;
    using System.Collections.Generic;
    using Unicast;

    /// <summary>
    /// Holds timeout information.
    /// </summary>
    public class TimeoutData 
    {
        /// <summary>
        /// The address of the client who requested the timeout.
        /// </summary>
        public Address Destination { get; set; }

        /// <summary>
        /// The saga ID.
        /// </summary>
        public Guid SagaId { get; set; }

        /// <summary>
        /// Additional state.
        /// </summary>
        public byte[] State { get; set; }

        /// <summary>
        /// The time at which the timeout expires.
        /// </summary>
        public DateTime Time { get; set; }
        
        /// <summary>
        /// The timeout manager that owns this particular timeout
        /// </summary>
        public string OwningTimeoutManager { get; set; }

        /// <summary>
        /// Store the headers to preserve them across timeouts
        /// </summary>
        public Dictionary<string, string> Headers { get; set; }

        /// <summary>
        /// Transforms the timeout to send options
        /// </summary>
        public SendOptions ToSendOptions()
        {
            var replyToAddress = Address.Local;
            if (Headers != null)
            {
                string originalReplyToAddressValue;
                if (Headers.TryGetValue(OriginalReplyToAddress, out originalReplyToAddressValue))
                {
                    replyToAddress = Address.Parse(originalReplyToAddressValue);
                    Headers.Remove(OriginalReplyToAddress);
                }
            }

            return new SendOptions(Destination)
            {
                ReplyToAddress = replyToAddress
            };
        }

        /// <summary>
        /// Original ReplyTo address header.
        /// </summary>
        public const string OriginalReplyToAddress = "NServiceBus.Timeout.ReplyToAddress";
    }
}
