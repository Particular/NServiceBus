namespace NServiceBus.Timeout.Core
{
    using System;
    using System.Collections.Generic;
    using NServiceBus;

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
        /// The time at which the saga ID expired.
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

        public override string ToString()
        {
            return string.Format("Timeout({0}) - Expires:{1}, SagaId:{2}",Id,Time,SagaId);
        }
    }
}
