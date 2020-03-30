namespace NServiceBus.ObjectBuilder.MsDI
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Common;
    using Janitor;
    using Microsoft.Extensions.DependencyInjection;

    [SkipWeaving]

    class MsDIObjectBuilder : IContainer
    {
        IServiceCollection serviceCollection = new ServiceCollection();
        IServiceProvider serviceProvider;
        IServiceScope serviceScope;

        public MsDIObjectBuilder()
        {
            serviceCollection = new ServiceCollection();
        }

        public MsDIObjectBuilder(IServiceScope serviceScope)
        {
            this.serviceScope = serviceScope;
            serviceProvider = serviceScope.ServiceProvider;
        }

        public object Build(Type typeToBuild)
        {
            BuildProviderIfNeeded();

            return serviceProvider.GetRequiredService(typeToBuild);
        }

        public IContainer BuildChildContainer()
        {
            return new MsDIObjectBuilder(serviceProvider.CreateScope());
        }

        public IEnumerable<object> BuildAll(Type typeToBuild)
        {
            BuildProviderIfNeeded();

            return serviceProvider.GetServices(typeToBuild);
        }

        public void Configure(Type component, DependencyLifecycle dependencyLifecycle)
        {
            ThrowIfCalledOnChildContainer();

            if (HasComponent(component))
            {
                return;
            }

            var serviceLifetime = GetLifeTime(dependencyLifecycle);

            serviceCollection.Add(new ServiceDescriptor(component, component, serviceLifetime));

            /*
            var interfaces = GetAllServices(component);

            foreach (var serviceType in interfaces)
            {
                container.Register(serviceType, s => s.GetInstance(component), serviceLifetime, component.FullName);
            }
            */
        }

        public void Configure<T>(Func<T> component, DependencyLifecycle dependencyLifecycle)
        {
            ThrowIfCalledOnChildContainer();

            var componentType = typeof(T);

            if (HasComponent(componentType))
            {
                return;
            }

            var serviceLifetime = GetLifeTime(dependencyLifecycle);

            serviceCollection.Add(new ServiceDescriptor(typeof(T), sp => component(), serviceLifetime));

            /*
            var interfaces = GetAllServices(componentType);

            foreach (var servicesType in interfaces)
            {
                container.Register(servicesType, s => s.GetInstance<T>(), serviceLifetime, componentType.FullName);
            }
            */
        }

        public void RegisterSingleton(Type lookupType, object instance)
        {
            serviceCollection.AddSingleton(lookupType, instance);
        }

        public bool HasComponent(Type componentType)
        {
            return serviceCollection.Any(sd => sd.ServiceType == componentType);
        }

        public void Release(object instance)
        {
            //no-op
        }

        public void Dispose()
        {
            if (serviceScope != null)
            {
                serviceScope.Dispose();
            }
            else
            {
                ((ServiceProvider)serviceProvider).Dispose();
            }
        }

        ServiceLifetime GetLifeTime(DependencyLifecycle dependencyLifecycle)
        {
            switch (dependencyLifecycle)
            {
                case DependencyLifecycle.InstancePerCall:
                    return ServiceLifetime.Transient;
                case DependencyLifecycle.InstancePerUnitOfWork:
                    return ServiceLifetime.Scoped;
                case DependencyLifecycle.SingleInstance:
                    return ServiceLifetime.Singleton;
                default:
                    throw new Exception();
            }
        }

        void BuildProviderIfNeeded()
        {
            if (serviceProvider == null)
            {
                serviceProvider = serviceCollection.BuildServiceProvider(true);
            }
        }

        void ThrowIfCalledOnChildContainer()
        {
            if (serviceScope != null)
            {
                throw new InvalidOperationException("Reconfiguration of child containers is not allowed.");
            }
        }
    }
}