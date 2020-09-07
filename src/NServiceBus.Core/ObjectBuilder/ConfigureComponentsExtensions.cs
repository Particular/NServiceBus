namespace NServiceBus
{
    using System;
    using Microsoft.Extensions.DependencyInjection;

    /// <summary>
    /// TODO
    /// </summary>
    public static class ConfigureComponentsExtensions
    {
        /// <summary>
        /// Configures the given type. Can be used to configure all kinds of properties.
        /// </summary>
        /// <param name="serviceCollection">The <see cref="IServiceCollection"/> to register the types in.</param>
        /// <param name="concreteComponent">The concrete implementation of the component.</param>
        /// <param name="dependencyLifecycle">Defines lifecycle semantics for the given type.</param>
        public static void ConfigureComponent(this IServiceCollection serviceCollection, Type concreteComponent, DependencyLifecycle dependencyLifecycle)
        {
        }

        /// <summary>
        /// Configures the given type, allowing to fluently configure properties.
        /// </summary>
        /// <param name="serviceCollection">The <see cref="IServiceCollection"/> to register the types in.</param>
        /// <param name="dependencyLifecycle">Defines lifecycle semantics for the given type.</param>
        public static void ConfigureComponent<T>(this IServiceCollection serviceCollection, DependencyLifecycle dependencyLifecycle)
        {

        }

        /// <summary>
        /// Configures the given type, allowing to fluently configure properties.
        /// </summary>
        /// <typeparam name="T">Type to configure.</typeparam>
        /// <param name="serviceCollection">The <see cref="IServiceCollection"/> to register the types in.</param>
        /// <param name="componentFactory">Factory method that returns the given type.</param>
        /// <param name="dependencyLifecycle">Defines lifecycle semantics for the given type.</param>
        public static void ConfigureComponent<T>(this IServiceCollection serviceCollection, Func<T> componentFactory, DependencyLifecycle dependencyLifecycle)
        {

        }

        /// <summary>
        /// Configures the given type, allowing to fluently configure properties.
        /// </summary>
        public static void ConfigureComponent<T>(this IServiceCollection serviceCollection, Func<IServiceProvider, T> componentFactory, DependencyLifecycle dependencyLifecycle)
        {

        }

        /// <summary>
        /// Registers the given instance as the singleton that will be returned
        /// for the given type.
        /// </summary>
        public static void RegisterSingleton(this IServiceCollection serviceCollection, Type lookupType, object instance)
        {

        }

        /// <summary>
        /// Registers the given instance as the singleton that will be returned
        /// for the given type.
        /// </summary>
        public static void RegisterSingleton<T>(this IServiceCollection serviceCollection, T instance)
        {

        }

        /// <summary>
        /// Indicates if a component of the given type has been configured.
        /// </summary>
        public static bool HasComponent<T>(this IServiceCollection serviceCollection)
        {
            return false;
        }

        /// <summary>
        /// Indicates if a component of the given type has been configured.
        /// </summary>
        public static bool HasComponent(this IServiceCollection serviceCollection, Type componentType)
        {
            return false;
        }
    }
}