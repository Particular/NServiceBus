namespace NServiceBus
{
    using System;
    using System.Linq;
    using Microsoft.Extensions.DependencyInjection;
    using ObjectBuilder;

    class CommonObjectBuilder : IConfigureComponents
    {
        public CommonObjectBuilder(IServiceCollection serviceCollection)
        {
            this.serviceCollection = serviceCollection;
        }


        public void ConfigureComponent(Type concreteComponent, DependencyLifecycle instanceLifecycle)
        {
            var serviceLifeTime = MapLifeCycle(instanceLifecycle);
            serviceCollection.Add(new ServiceDescriptor(concreteComponent, concreteComponent, serviceLifeTime));
            RegisterInterfaces(concreteComponent, serviceLifeTime);
        }

        public void ConfigureComponent<T>(DependencyLifecycle instanceLifecycle)
        {
            ConfigureComponent(typeof(T), instanceLifecycle);
        }

        public void ConfigureComponent<T>(Func<T> componentFactory, DependencyLifecycle instanceLifecycle)
        {
            ConfigureComponent(_ => componentFactory(), instanceLifecycle);
        }

        public void ConfigureComponent<T>(Func<IServiceProvider, T> componentFactory, DependencyLifecycle instanceLifecycle)
        {
            var componentType = typeof(T);
            var serviceLifeTime = MapLifeCycle(instanceLifecycle);
            serviceCollection.Add(new ServiceDescriptor(componentType, p => componentFactory(p), serviceLifeTime));
            RegisterInterfaces(componentType, serviceLifeTime);
        }

        public void RegisterSingleton(Type lookupType, object instance)
        {
            serviceCollection.AddSingleton(lookupType, instance);
        }

        public void RegisterSingleton<T>(T instance)
        {
            RegisterSingleton(typeof(T), instance);
        }

        public bool HasComponent<T>()
        {
            return HasComponent(typeof(T));
        }

        public bool HasComponent(Type componentType)
        {
            return serviceCollection.Any(sd => sd.ServiceType == componentType);
        }

        void RegisterInterfaces(Type component, ServiceLifetime lifetime)
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
                case DependencyLifecycle.InstancePerCall: return ServiceLifetime.Transient;
                case DependencyLifecycle.SingleInstance: return ServiceLifetime.Singleton;
                case DependencyLifecycle.InstancePerUnitOfWork: return ServiceLifetime.Scoped;
                default: throw new NotSupportedException($"{dependencyLifecycle} is not supported.");
            }
        }

        IServiceCollection serviceCollection;
    }
}