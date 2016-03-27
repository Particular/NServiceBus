namespace NServiceBus
{
    /// <summary>
    /// Represent the various lifecycles available for components configured in the container.
    /// </summary>
    public enum DependencyLifecycle
    {
        /// <summary>
        /// The same instance will be returned each time.
        /// </summary>
        SingleInstance,

        /// <summary>
        /// The instance will be singleton for the duration of the unit of work. In practice this means
        /// the processing of a single transport message.
        /// </summary>
        InstancePerUnitOfWork,

        /// <summary>
        /// A new instance will be returned for each call.
        /// </summary>
        InstancePerCall
    }
}