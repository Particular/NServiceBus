namespace NServiceBus.Pipeline.Contexts
{
    using ObjectBuilder;

    /// <summary>
    /// Acts as a root context for context chains.
    /// </summary>
    class RootContext : BehaviorContextImpl
    {
        /// <summary>
        /// Create a new instance which uses the provided builder and settings.
        /// </summary>
        public RootContext(IBuilder builder) : base(null)
        {
            //TODO DanielTim: Should we guard parameters against null and provide a FakeContext for testing?
            Set(builder);
        }
    }
}