﻿namespace NServiceBus.Pipeline
{
    using Extensibility;
    using ObjectBuilder;

    /// <summary>
    /// Base interface for a pipeline behavior.
    /// </summary>
    public interface IBehaviorContext : IExtendable
    {
        /// <summary>
        /// The current <see cref="IChildBuilder"/>.
        /// </summary>
        IChildBuilder Builder { get; }
    }
}