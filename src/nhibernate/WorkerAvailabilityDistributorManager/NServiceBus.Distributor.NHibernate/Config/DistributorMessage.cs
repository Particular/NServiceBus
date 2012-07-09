namespace NServiceBus.Distributor.NHibernate.Config
{
    using System;

    /// <summary>
    /// Distributor message.
    /// </summary>
    public class DistributorMessage
    {
        /// <summary>
        /// Id of this timeout.
        /// </summary>
        public virtual int Id { get; set; }

        /// <summary>
        /// The address of the client who requested the timeout.
        /// </summary>
        public virtual Address Destination { get; set; }

        /// <summary>
        /// Timeout endpoint name.
        /// </summary>
        public virtual string Endpoint { get; set; }
    }
}
