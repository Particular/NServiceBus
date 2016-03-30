namespace NServiceBus.ObjectBuilder.Common
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Abstraction of a container.
    /// </summary>
    public interface IContainer : IDisposable
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
        IContainer BuildChildContainer();

        /// <summary>
        /// Returns a list of objects instantiated because their type is compatible
        /// with the given type.
        /// </summary>
        /// <param name="typeToBuild">Type to be build.</param>
        /// <returns>Enumeration of all types that implement <paramref name="typeToBuild" />.</returns>
        IEnumerable<object> BuildAll(Type typeToBuild);

        /// <summary>
        /// Configures the call model of the given component type.
        /// </summary>
        /// <param name="component">Type to be configured.</param>
        /// <param name="dependencyLifecycle">The desired lifecycle for this type.</param>
        void Configure(Type component, DependencyLifecycle dependencyLifecycle);

        /// <summary>
        /// Configures the call model of the given component type using a <see cref="Func{T}" />.
        /// </summary>
        /// <typeparam name="T">Type to be configured.</typeparam>
        /// <param name="component"><see cref="Func{T}" /> to use to configure.</param>
        /// <param name="dependencyLifecycle">The desired lifecycle for this type.</param>
        void Configure<T>(Func<T> component, DependencyLifecycle dependencyLifecycle);

        /// <summary>
        /// Registers the given instance as the singleton that will be returned for the given type.
        /// </summary>
        /// <param name="lookupType">The interface type.</param>
        /// <param name="instance">The implementation instance.</param>
        void RegisterSingleton(Type lookupType, object instance);

        /// <summary>
        /// Indicates if a component of the given type has been configured.
        /// </summary>
        /// <param name="componentType">Component type to check.</param>
        /// <returns><c>true</c> if the <paramref name="componentType" /> is registered in the container or <c>false</c> otherwise.</returns>
        bool HasComponent(Type componentType);

        /// <summary>
        /// Releases a component instance.
        /// </summary>
        /// <param name="instance">The component instance to release.</param>
        void Release(object instance);
    }
}