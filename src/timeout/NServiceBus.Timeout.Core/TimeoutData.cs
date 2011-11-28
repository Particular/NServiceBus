namespace NServiceBus.Timeout.Core
{
    using System;
    using NServiceBus;

    /// <summary>
    /// Holds timeout information.
    /// </summary>
    public class TimeoutData : EventArgs
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
        public object State { get; set; }

        /// <summary>
        /// The time at which the saga ID expired.
        /// </summary>
        public DateTime Time { get; set; }
    }
}
