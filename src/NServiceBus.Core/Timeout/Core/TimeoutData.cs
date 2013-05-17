namespace NServiceBus.Timeout.Core
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Holds timeout information.
    /// </summary>
    public class TimeoutData : EventArgs
    {
        /// <summary>
        /// Id of this timeout
        /// </summary>
        public string Id { get; set; }

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
        /// We store the correlation id in order to preserve it across timeouts
        /// </summary>
        public string CorrelationId { get; set; }

        /// <summary>
        /// The timeout manager that owns this particular timeout
        /// </summary>
        public string OwningTimeoutManager { get; set; }

        /// <summary>
        /// Store the headers to preserve them across timeouts
        /// </summary>
        public Dictionary<string, string> Headers { get; set; }

        /// <summary>
        /// Returns a <see cref="T:System.String"/> that represents the current <see cref="T:System.Object"/>.
        /// </summary>
        /// <returns>
        /// A <see cref="T:System.String"/> that represents the current <see cref="T:System.Object"/>.
        /// </returns>
        /// <filterpriority>2</filterpriority>
        public override string ToString()
        {
            return string.Format("Timeout({0}) - Expires:{1}, SagaId:{2}", Id, Time, SagaId);
        }

        /// <summary>
        /// Transforms the timeout to a <see cref="TransportMessage"/>.
        /// </summary>
        /// <returns>Returns a <see cref="TransportMessage"/>.</returns>
        public TransportMessage ToTransportMessage()
        {
            var replyToAddress = Address.Local;
            if (Headers != null && Headers.ContainsKey(OriginalReplyToAddress))
            {
                replyToAddress = Address.Parse(Headers[OriginalReplyToAddress]);
                Headers.Remove(OriginalReplyToAddress);
            }

            var transportMessage = new TransportMessage(Id,Headers)
            {
                ReplyToAddress = replyToAddress,
                Recoverable = true,
                CorrelationId = CorrelationId,
                Body = State
            };


            if (SagaId != Guid.Empty)
            {
                transportMessage.Headers[NServiceBus.Headers.SagaId] = SagaId.ToString();
            }


            transportMessage.Headers["NServiceBus.RelatedToTimeoutId"] = Id;

            return transportMessage;
        }

        /// <summary>
        /// Original ReplyTo address header.
        /// </summary>
        public const string OriginalReplyToAddress = "NServiceBus.Timeout.ReplyToAddress";
    }
}
