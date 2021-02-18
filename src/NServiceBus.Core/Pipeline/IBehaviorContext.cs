namespace NServiceBus.Pipeline
{
    using System;
    using Extensibility;

    /// <summary>
    /// Base interface for a pipeline behavior.
    /// </summary>
    public interface IBehaviorContext : ICancellableContext, IExtendable
    {
        /// <summary>
        /// The current <see cref="IServiceProvider" />.
        /// </summary>
        IServiceProvider Builder { get; }
    }
}