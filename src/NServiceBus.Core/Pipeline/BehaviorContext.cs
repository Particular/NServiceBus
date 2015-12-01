namespace NServiceBus.Pipeline
{
    using Extensibility;
    using ObjectBuilder;

    /// <summary>
    /// Base interface for a pipeline behavior.
    /// </summary>
    public interface BehaviorContext : IExtendable
    {
        /// <summary>
        /// The current <see cref="IBuilder"/>.
        /// </summary>
        IBuilder Builder { get; }
    }
}