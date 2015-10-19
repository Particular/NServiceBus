namespace NServiceBus.Routing
{
    /// <summary>
    /// Contains information about the endpoint instance.
    /// </summary>
    public class EndpointInstanceData
    {
        /// <summary>
        /// The name of the instance.
        /// </summary>
        public EndpointInstanceName Name { get; }

        /// <summary>
        /// Creates new endpoint data object.
        /// </summary>
        /// <param name="name">Name of the endpoint instance.</param>
        public EndpointInstanceData(EndpointInstanceName name)
        {
            Name = name;
        }
    }
}