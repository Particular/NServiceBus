namespace NServiceBus.ObjectBuilder
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Used to instantiate types, so that all configured dependencies
    /// and property values are set.
    /// An abstraction on top of dependency injection frameworks.
    /// </summary>
    public interface IBuilder : IChildBuilder
    {
        /// <summary>
        /// Returns a child instance of the container to facilitate deterministic disposal
        /// of all resources built by the child container.
        /// </summary>
        /// <returns>Returns a new child container.</returns>
        IChildBuilder CreateChildBuilder();
    }
}
