namespace NServiceBus.ObjectBuilder
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// asdasdsad ads asd asd
    /// </summary>
    /// <seealso cref="IBuilder" />
    public interface IChildBuilder : IDisposable
    {
        /// <summary>
        /// Returns an instantiation of the given type.
        /// </summary>
        /// <param name="typeToBuild">The <see cref="Type"/> to build.</param>
        /// <returns>The component instance.</returns>
        object Build(Type typeToBuild);

        /// <summary>
        /// Creates an instance of the given type, injecting it with all defined dependencies.
        /// </summary>
        /// <typeparam name="T">Type to be resolved.</typeparam>
        /// <returns>Instance of <typeparamref name="T"/>.</returns>
        T Build<T>();

        /// <summary>
        /// For each type that is compatible with T, an instance is created with all dependencies injected, and yielded to the caller.
        /// </summary>
        /// <typeparam name="T">Type to be resolved.</typeparam>
        /// <returns>Instances of <typeparamref name="T"/>.</returns>
        IEnumerable<T> BuildAll<T>();

        /// <summary>
        /// For each type that is compatible with the given type, an instance is created with all dependencies injected.
        /// </summary>
        /// <param name="typeToBuild">The <see cref="Type"/> to build.</param>
        /// <returns>The component instances.</returns>
        IEnumerable<object> BuildAll(Type typeToBuild);

        /// <summary>
        /// Releases a component instance.
        /// </summary>
        /// <param name="instance">The component instance to release.</param>
        void Release(object instance);

        /// <summary>
        /// Builds an instance of the defined type injecting it with all defined dependencies
        /// and invokes the given action on the instance.
        /// </summary>
        /// <param name="typeToBuild">The <see cref="Type"/> to build.</param>
        /// <param name="action">The callback to call.</param>
        void BuildAndDispatch(Type typeToBuild, Action<object> action);
    }
}
