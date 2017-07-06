namespace NServiceBus.ObjectBuilder
{
    using System;

    /// <summary>
    /// Used to instantiate types that need a container instance to create
    /// </summary>
    public interface IResolver
    {
        /// <summary>
        /// Returns an instantiation of the given type.
        /// </summary>
        /// <param name="typeToBuild">The <see cref="Type" /> to build.</param>
        /// <returns>The component instance.</returns>
        object Build(Type typeToBuild);

        /// <summary>
        /// Creates an instance of the given type, injecting it with all defined dependencies.
        /// </summary>
        /// <typeparam name="T">Type to be resolved.</typeparam>
        /// <returns>Instance of <typeparamref name="T" />.</returns>
        T Build<T>();
    }
}