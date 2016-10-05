namespace NServiceBus.Timeout.Core
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Holds timeout information.
    /// </summary>
    public partial class TimeoutData
    {
        /// <summary>
        /// Id of this timeout.
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// The address of the client who requested the timeout.
        /// </summary>
        public string Destination { get; set; }

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
        /// The timeout manager that owns this particular timeout.
        /// </summary>
        public string OwningTimeoutManager { get; set; }

        /// <summary>
        /// Store the headers to preserve them across timeouts.
        /// </summary>
        public Dictionary<string, string> Headers { get; set; }

        /// <summary>
        /// Returns a <see cref="String" /> that represents the current <see cref="Object" />.
        /// </summary>
        /// <returns>
        /// A <see cref="String" /> that represents the current <see cref="Object" />.
        /// </returns>
        public override string ToString()
        {
            return $"Timeout({Id}) - Expires:{Time}, SagaId:{SagaId}";
        }
    }
}