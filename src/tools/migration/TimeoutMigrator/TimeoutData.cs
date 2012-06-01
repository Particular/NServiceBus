namespace TimeoutMigrator
{
    using System;

    public class TimeoutData : EventArgs
    {
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
        public object State { get; set; }

        /// <summary>
        /// The time at which the saga ID expired.
        /// </summary>
        public DateTime Time { get; set; }

        /// <summary>
        /// The MSMQ message lookup value - set after storage occurs.
        /// </summary>
        public string MessageId { get; set; }
    }
}