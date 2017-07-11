namespace NServiceBus.ObjectBuilder
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Used to instantiate types, so that all configured dependencies
    /// and property values are set.
    /// An abstraction on top of dependency injection frameworks.
    /// </summary>
    public interface IBuilder : IDisposable
    {
        /// <summary>
        /// Returns an instantiation of the given type.
        /// </summary>
        /// <param name="typeToBuild">The <see cref="Type" /> to build.</param>
        /// <returns>The component instance.</returns>
        object Build(Type typeToBuild);

        /// <summary>
        /// Returns a child instance of the container to facilitate deterministic disposal
        /// of all resources built by the child container.
        /// </summary>
        /// <returns>Returns a new child container.</returns>
        IBuilder CreateChildBuilder();

        /// <summary>
        /// Creates an instance of the given type, injecting it with all defined dependencies.
        /// </summary>
        /// <typeparam name="T">Type to be resolved.</typeparam>
        /// <returns>Instance of <typeparamref name="T" />.</returns>
        T Build<T>();

        /// <summary>
        /// For each type that is compatible with T, an instance is created with all dependencies injected, and yielded to the
        /// caller.
        /// </summary>
        /// <typeparam name="T">Type to be resolved.</typeparam>
        /// <returns>Instances of <typeparamref name="T" />.</returns>
        IEnumerable<T> BuildAll<T>();

        /// <summary>
        /// For each type that is compatible with the given type, an instance is created with all dependencies injected.
        /// </summary>
        /// <param name="typeToBuild">The <see cref="Type" /> to build.</param>
        /// <returns>The component instances.</returns>
        IEnumerable<object> BuildAll(Type typeToBuild);
    }
}