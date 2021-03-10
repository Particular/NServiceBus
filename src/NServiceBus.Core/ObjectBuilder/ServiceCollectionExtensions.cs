namespace NServiceBus
{
    using System;
    using System.Linq;
    using Microsoft.Extensions.DependencyInjection;
    using ObjectBuilder;

    /// <summary>
    /// Contains extension methods for <see cref="IServiceCollection"/> that were formerly provided by <see cref="IConfigureComponents"/>.
    /// </summary>
    [ObsoleteEx(
        Message = "Use methods on IServiceCollection instead.",
        TreatAsErrorFromVersion = "9.0",
        RemoveInVersion = "10.0")]
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Configures the given type. Can be used to configure all kinds of properties.
        /// </summary>
        /// <param name="serviceCollection">The <see cref="IServiceCollection"/> to register the types in.</param>
        /// <param name="concreteComponent">The concrete implementation of the component.</param>
        /// <param name="dependencyLifecycle">Defines lifecycle semantics for the given type.</param>
        [ObsoleteEx(
            ReplacementTypeOrMember = "IServiceCollection.Add",
            TreatAsErrorFromVersion = "9.0",
            RemoveInVersion = "10.0")]
        public static void ConfigureComponent(this IServiceCollection serviceCollection, Type concreteComponent, DependencyLifecycle dependencyLifecycle)
        {
            var serviceLifeTime = MapLifeCycle(dependencyLifecycle);
            serviceCollection.Add(new ServiceDescriptor(concreteComponent, concreteComponent, serviceLifeTime));
            RegisterInterfaces(concreteComponent, serviceLifeTime, serviceCollection);
        }

        /// <summary>
        /// Configures the given type, allowing to fluently configure properties.
        /// </summary>
        /// <param name="serviceCollection">The <see cref="IServiceCollection"/> to register the types in.</param>
        /// <param name="dependencyLifecycle">Defines lifecycle semantics for the given type.</param>
        [ObsoleteEx(
            Message = "Use `IServiceCollection.Add`, `IServiceCollection.AddSingleton`, `IServiceCollection.AddTransient` or `IServiceCollection.AddScoped` instead.",
            TreatAsErrorFromVersion = "9.0",
            RemoveInVersion = "10.0")]
        public static void ConfigureComponent<T>(this IServiceCollection serviceCollection, DependencyLifecycle dependencyLifecycle)
        {
            serviceCollection.ConfigureComponent(typeof(T), dependencyLifecycle);
        }

        /// <summary>
        /// Configures the given type, allowing to fluently configure properties.
        /// </summary>
        /// <typeparam name="T">Type to configure.</typeparam>
        /// <param name="serviceCollection">The <see cref="IServiceCollection"/> to register the types in.</param>
        /// <param name="componentFactory">Factory method that returns the given type.</param>
        /// <param name="dependencyLifecycle">Defines lifecycle semantics for the given type.</param>
        [ObsoleteEx(
            Message = "Use `IServiceCollection.Add`, `IServiceCollection.AddSingleton`, `IServiceCollection.AddTransient` or `IServiceCollection.AddScoped` instead.",
            TreatAsErrorFromVersion = "9.0",
            RemoveInVersion = "10.0")]
        public static void ConfigureComponent<T>(this IServiceCollection serviceCollection, Func<T> componentFactory, DependencyLifecycle dependencyLifecycle)
        {
            serviceCollection.ConfigureComponent(_ => componentFactory(), dependencyLifecycle);
        }

        /// <summary>
        /// Configures the given type, allowing to fluently configure properties.
        /// </summary>
        [ObsoleteEx(
            Message = "Use `IServiceCollection.Add`, `IServiceCollection.AddSingleton`, `IServiceCollection.AddTransient` or `IServiceCollection.AddScoped` instead.",
            TreatAsErrorFromVersion = "9.0",
            RemoveInVersion = "10.0")]
        public static void ConfigureComponent<T>(this IServiceCollection serviceCollection, Func<IServiceProvider, T> componentFactory, DependencyLifecycle dependencyLifecycle)
        {
            var componentType = typeof(T);
            var serviceLifeTime = MapLifeCycle(dependencyLifecycle);
            serviceCollection.Add(new ServiceDescriptor(componentType, p => componentFactory(p), serviceLifeTime));
            RegisterInterfaces(componentType, serviceLifeTime, serviceCollection);
        }

        /// <summary>
        /// Registers the given instance as the singleton that will be returned
        /// for the given type.
        /// </summary>
        [ObsoleteEx(
            Message = "Use `IServiceCollection.Add`, `IServiceCollection.AddSingleton`, `IServiceCollection.AddTransient` or `IServiceCollection.AddScoped` instead.",
            TreatAsErrorFromVersion = "9.0",
            RemoveInVersion = "10.0")]
        public static void RegisterSingleton(this IServiceCollection serviceCollection, Type lookupType, object instance)
        {
            serviceCollection.AddSingleton(lookupType, instance);
        }

        /// <summary>
        /// Registers the given instance as the singleton that will be returned
        /// for the given type.
        /// </summary>
        [ObsoleteEx(
            ReplacementTypeOrMember = "IServiceCollection.AddSingleton",
            TreatAsErrorFromVersion = "9.0",
            RemoveInVersion = "10.0")]
        public static void RegisterSingleton<T>(this IServiceCollection serviceCollection, T instance)
        {
            serviceCollection.RegisterSingleton(typeof(T), instance);
        }

        /// <summary>
        /// Indicates if a component of the given type has been configured.
        /// </summary>
        [ObsoleteEx(
            ReplacementTypeOrMember = "IServiceCollection.GetEnumerator",
            TreatAsErrorFromVersion = "9.0",
            RemoveInVersion = "10.0")]
        public static bool HasComponent<T>(this IServiceCollection serviceCollection)
        {
            return serviceCollection.HasComponent(typeof(T));
        }

        /// <summary>
        /// Indicates if a component of the given type has been configured.
        /// </summary>
        [ObsoleteEx(
            ReplacementTypeOrMember = "IServiceCollection.GetEnumerator",
            TreatAsErrorFromVersion = "9.0",
            RemoveInVersion = "10.0")]
        public static bool HasComponent(this IServiceCollection serviceCollection, Type componentType)
        {
            return serviceCollection.Any(sd => sd.ServiceType == componentType);
        }

        static void RegisterInterfaces(Type component, ServiceLifetime lifetime, IServiceCollection serviceCollection)
        {
            var interfaces = component.GetInterfaces();
            foreach (var serviceType in interfaces)
            {
                // see https://andrewlock.net/how-to-register-a-service-with-multiple-interfaces-for-in-asp-net-core-di/
                serviceCollection.Add(new ServiceDescriptor(serviceType, sp => sp.GetService(component), lifetime));
            }
        }

        static ServiceLifetime MapLifeCycle(DependencyLifecycle dependencyLifecycle)
        {
            switch (dependencyLifecycle)
            {
                case DependencyLifecycle.InstancePerCall:
                    return ServiceLifetime.Transient;
                case DependencyLifecycle.SingleInstance:
                    return ServiceLifetime.Singleton;
                case DependencyLifecycle.InstancePerUnitOfWork:
                    return ServiceLifetime.Scoped;
                default:
                    throw new NotSupportedException($"{dependencyLifecycle} is not supported.");
            }
        }
    }
}