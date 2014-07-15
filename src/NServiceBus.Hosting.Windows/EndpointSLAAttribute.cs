namespace NServiceBus.Hosting
{
    using System;

    /// <summary>
    /// Defines the SLA for this endpoint. Needs to be set on the endpoint configuration class
    /// </summary>
    public sealed class EndpointSLAAttribute : Attribute
    {
        /// <summary>
        /// Used to define the SLA for this endpoint
        /// </summary>
        /// <param name="sla">A <see cref="string"/> representing a <see cref="TimeSpan"/></param>
        public EndpointSLAAttribute(string sla)
        {
            SLA = sla;
        }

        /// <summary>
        /// The SLA of the endpoint.
        /// </summary>
        public string SLA { get; set; }
    }
}