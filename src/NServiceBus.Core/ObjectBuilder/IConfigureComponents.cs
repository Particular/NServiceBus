namespace NServiceBus.ObjectBuilder
{
    using System;
    using System.Linq.Expressions;

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
        IComponentConfig ConfigureComponent(Type concreteComponent, DependencyLifecycle dependencyLifecycle);

        /// <summary>
        /// Configures the given type, allowing to fluently configure properties.
        /// </summary>
        /// <param name="dependencyLifecycle">Defines lifecycle semantics for the given type.</param>
        IComponentConfig<T> ConfigureComponent<T>(DependencyLifecycle dependencyLifecycle);

        /// <summary>
        /// Configures the given type, allowing to fluently configure properties.
        /// </summary>
        /// <typeparam name="T">Type to configure.</typeparam>
        /// <param name="componentFactory">Factory method that returns the given type.</param>
        /// <param name="dependencyLifecycle">Defines lifecycle semantics for the given type.</param>
        IComponentConfig<T> ConfigureComponent<T>(Func<T> componentFactory, DependencyLifecycle dependencyLifecycle);

        /// <summary>
        /// Configures the given type, allowing to fluently configure properties.
        /// </summary>
        IComponentConfig<T> ConfigureComponent<T>(Func<IBuilder,T> componentFactory, DependencyLifecycle dependencyLifecycle);

        /// <summary>
        /// Configures the given property of the given type to be injected with the given value.
        /// </summary>
        IConfigureComponents ConfigureProperty<T>(Expression<Func<T, object>> property, object value);

        /// <summary>
        /// Configures the given property of the given type to be injected with the given value.
        /// </summary>
        IConfigureComponents ConfigureProperty<T>(string propertyName, object value);

        /// <summary>
        /// Registers the given instance as the singleton that will be returned
        /// for the given type.
        /// </summary>
        IConfigureComponents RegisterSingleton(Type lookupType, object instance);

        /// <summary>
        /// Registers the given instance as the singleton that will be returned
        /// for the given type.
        /// </summary>
        IConfigureComponents RegisterSingleton<T>(T instance);

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
