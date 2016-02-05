namespace NServiceBus.ObjectBuilder.Common
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Parent container which can configure registrations and build child containers
    /// </summary>
    /// <seealso cref="IChildContainer" />
    public interface IContainer : IChildContainer
    {
        /// <summary>
        /// Returns a child instance of the container to facilitate deterministic disposal
        /// of all resources built by the child container.
        /// </summary>
        /// <returns>Returns a new child container.</returns>
        IChildContainer BuildChildContainer();

        /// <summary>
        /// Configures the call model of the given component type.
        /// </summary>
        /// <param name="component">Type to be configured.</param>
        /// <param name="dependencyLifecycle">The desired lifecycle for this type.</param>
        void Configure(Type component, DependencyLifecycle dependencyLifecycle);

        /// <summary>
        /// Configures the call model of the given component type using a <see cref="Func{T}"/>.
        /// </summary>
        /// <typeparam name="T">Type to be configured.</typeparam>
        /// <param name="component"><see cref="Func{T}"/> to use to configure.</param>
        /// <param name="dependencyLifecycle">The desired lifecycle for this type.</param>
        void Configure<T>(Func<T> component, DependencyLifecycle dependencyLifecycle);

        /// <summary>
        /// Sets the value to be configured for the given property of the 
        /// given component type.
        /// </summary>
        /// <param name="component">The interface type.</param>
        /// <param name="property">The property name to be injected.</param>
        /// <param name="value">The value to assign to the <paramref name="property"/>.</param>
        void ConfigureProperty(Type component, string property, object value);

        /// <summary>
        /// Registers the given instance as the singleton that will be returned for the given type.
        /// </summary>
        /// <param name="lookupType">The interface type.</param>
        /// <param name="instance">The implementation instance.</param>
        void RegisterSingleton(Type lookupType, object instance);
    }
}