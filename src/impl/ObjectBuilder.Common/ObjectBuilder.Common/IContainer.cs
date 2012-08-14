using System;
using System.Collections.Generic;

namespace NServiceBus.ObjectBuilder.Common
{
    /// <summary>
    /// Abstraction of a container.
    /// </summary>
    public interface IContainer : IDisposable
    {
        /// <summary>
        /// Returns an instantiation of the given type.
        /// </summary>
        /// <param name="typeToBuild"></param>
        /// <returns></returns>
        object Build(Type typeToBuild);

        /// <summary>
        /// Returns a child instance of the container to facilitate deterministic disposal
        /// of all resources built by the child container.
        /// </summary>
        /// <returns></returns>
        IContainer BuildChildContainer();

        /// <summary>
        /// Returns a list of objects instantiated because their type is compatible
        /// with the given type.
        /// </summary>
        /// <param name="typeToBuild"></param>
        /// <returns></returns>
        IEnumerable<object> BuildAll(Type typeToBuild);

        /// <summary>
        /// Configures the call model of the given component type.
        /// </summary>
        /// <param name="component">Type to be configured</param>
        /// <param name="dependencyLifecycle">The desired lifecycle for this type</param>
        void Configure(Type component, DependencyLifecycle dependencyLifecycle);

        /// <summary>
        /// Configures the call model of the given component type using a <see cref="Func{T}"/>.
        /// </summary>
        /// <typeparam name="T">Type to be configured</typeparam>
        /// <param name="component"><see cref="Func{T}"/> to use to configure.</param>
        /// <param name="dependencyLifecycle">The desired lifecycle for this type</param>
        void Configure<T>(Func<T> component, DependencyLifecycle dependencyLifecycle);

        /// <summary>
        /// Sets the value to be configured for the given property of the 
        /// given component type.
        /// </summary>
        /// <param name="component"></param>
        /// <param name="property"></param>
        /// <param name="value"></param>
        void ConfigureProperty(Type component, string property, object value);

        /// <summary>
        /// Registers the given instance as the singleton that will be returned
        /// for the given type.
        /// </summary>
        /// <param name="lookupType"></param>
        /// <param name="instance"></param>
        void RegisterSingleton(Type lookupType, object instance);

        /// <summary>
        /// Indicates if a component of the given type has been configured.
        /// </summary>
        /// <param name="componentType"></param>
        /// <returns></returns>
        bool HasComponent(Type componentType);
    }
}
