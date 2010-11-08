namespace NServiceBus.ObjectBuilder
{
    /// <summary>
    /// Represent the various call models for a component.
    /// </summary>
    public enum ComponentCallModelEnum
    {
        /// <summary>
        /// Accept the default call model of the underlying technology.
        /// </summary>
        None,
        /// <summary>
        /// Only one instance of the component will ever be called.
        /// </summary>
        Singleton,
        /// <summary>
        /// Each call on the component will be performed on a new instance.
        /// </summary>
        Singlecall
    }
}
