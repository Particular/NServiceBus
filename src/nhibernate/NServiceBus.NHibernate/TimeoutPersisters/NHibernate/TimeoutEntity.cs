namespace NServiceBus.TimeoutPersisters.NHibernate
{
    using System;
    using Timeout.Core;

    /// <summary>
    /// NHibernate wrapper class for <see cref="TimeoutData"/>
    /// </summary>
    public class TimeoutEntity
    {
        /// <summary>
        /// Id of this timeout.
        /// </summary>
        public virtual Guid Id { get; set; }

        /// <summary>
        /// The address of the client who requested the timeout.
        /// </summary>
        public virtual Address Destination { get; set; }

        /// <summary>
        /// The saga ID.
        /// </summary>
        public virtual Guid SagaId { get; set; }

        /// <summary>
        /// Additional state.
        /// </summary>
        public virtual byte[] State { get; set; }

        /// <summary>
        /// The time at which the saga ID expired.
        /// </summary>
        public virtual DateTime Time { get; set; }

        /// <summary>
        /// We store the correlation id in order to preserve it across timeouts.
        /// </summary>
        public virtual string CorrelationId { get; set; }

        /// <summary>
        /// Store the headers to preserve them across timeouts.
        /// </summary>
        public virtual string Headers { get; set; }

        /// <summary>
        /// Timeout endpoint name.
        /// </summary>
        public virtual string Endpoint { get; set; }
    }
}
