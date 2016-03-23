namespace NServiceBus.ObjectBuilder
{
    using System;

    /// <summary>
    /// Used to configure components in the container.
    /// Should primarily be used at startup/initialization time.
    /// </summary>
    public interface IConfigureComponents
    {
        /// <summary>
        /// Configures the given type. Can be used to configure all kinds of properties.
        /// </summary>
        /// <param name="concreteComponent">The concrete implementation of the component.</param>
        /// <param name="dependencyLifecycle">Defines lifecycle semantics for the given type.</param>
        void ConfigureComponent(Type concreteComponent, DependencyLifecycle dependencyLifecycle);

        /// <summary>
        /// Configures the given type, allowing to fluently configure properties.
        /// </summary>
        /// <param name="dependencyLifecycle">Defines lifecycle semantics for the given type.</param>
        void ConfigureComponent<T>(DependencyLifecycle dependencyLifecycle);

        /// <summary>
        /// Configures the given type, allowing to fluently configure properties.
        /// </summary>
        /// <typeparam name="T">Type to configure.</typeparam>
        /// <param name="componentFactory">Factory method that returns the given type.</param>
        /// <param name="dependencyLifecycle">Defines lifecycle semantics for the given type.</param>
        void ConfigureComponent<T>(Func<T> componentFactory, DependencyLifecycle dependencyLifecycle);

        /// <summary>
        /// Configures the given type, allowing to fluently configure properties.
        /// </summary>
        void ConfigureComponent<T>(Func<IBuilder, T> componentFactory, DependencyLifecycle dependencyLifecycle);

        /// <summary>
        /// Registers the given instance as the singleton that will be returned
        /// for the given type.
        /// </summary>
        void RegisterSingleton(Type lookupType, object instance);

        /// <summary>
        /// Registers the given instance as the singleton that will be returned
        /// for the given type.
        /// </summary>
        void RegisterSingleton<T>(T instance);

        /// <summary>
        /// Indicates if a component of the given type has been configured.
        /// </summary>
        bool HasComponent<T>();

        /// <summary>
        /// Indicates if a component of the given type has been configured.
        /// </summary>
        bool HasComponent(Type componentType);
    }
}