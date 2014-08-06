namespace NServiceBus
{
    using System;

    /// <summary>
    /// Defines the SLA for this endpoint. Needs to be set on the endpoint configuration class
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public sealed class EndpointSLAAttribute : Attribute
    {
        /// <summary>
        /// Used to define the SLA for this endpoint
        /// </summary>
        /// <param name="sla">A <see cref="string"/> representing a <see cref="TimeSpan"/></param>
        public EndpointSLAAttribute(string sla)
        {
            TimeSpan timespan;
            if (!TimeSpan.TryParse(sla, out timespan))
            {
                throw new InvalidOperationException("A invalid SLA string has been defined - " + sla);
            }
            if (timespan <= TimeSpan.Zero)
            {
                throw new InvalidOperationException("A invalid SLA string has been defined. It must be a positive timespan. - " + sla);
            }
            SLA = timespan;
        }

        /// <summary>
        /// The SLA of the endpoint.
        /// </summary>
        public TimeSpan SLA { get; private set; }
    }
}