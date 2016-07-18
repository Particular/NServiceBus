namespace NServiceBus
{
    using Routing;

    /// <summary>
    /// Provides MSMQ-specific extensions to routing.
    /// </summary>
    public static class EndpointInstanceExtensions
    {
        /// <summary>
        /// Returns an endpoint instance bound to a given machine name.
        /// </summary>
        /// <param name="instance">A plain instance.</param>
        /// <param name="machineName">Machine name.</param>
        public static EndpointInstance AtMachine(this EndpointInstance instance, string machineName)
        {
            return new EndpointInstance(instance.Endpoint, instance.InstanceName + $"@{machineName}");
        }
    }
}