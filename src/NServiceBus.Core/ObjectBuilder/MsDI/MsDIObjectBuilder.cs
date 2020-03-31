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
        ServiceProvider container;

        IServiceScope serviceScope;

        public MsDIObjectBuilder()
        {
            serviceCollection = new ServiceCollection();
        }

        MsDIObjectBuilder(IServiceScope serviceScope)
        {
            this.serviceScope = serviceScope;
        }

        public object Build(Type typeToBuild)
        {
            MakeSureScopeInitialized();

            return serviceScope.ServiceProvider.GetRequiredService(typeToBuild);
        }

        public IContainer BuildChildContainer()
        {
            MakeSureScopeInitialized();

            return new MsDIObjectBuilder(serviceScope.ServiceProvider.CreateScope());
        }

        public IEnumerable<object> BuildAll(Type typeToBuild)
        {
            MakeSureScopeInitialized();

            return serviceScope.ServiceProvider.GetServices(typeToBuild);
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

            
            var interfaces = GetAllServices(component);

            foreach (var serviceType in interfaces)
            {
                serviceCollection.Add(new ServiceDescriptor(serviceType, sp => sp.GetRequiredService(component), serviceLifetime));
            }
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

            var interfaces = GetAllServices(componentType);

            foreach (var servicesType in interfaces)
            {
                serviceCollection.Add(new ServiceDescriptor(servicesType, sp => component(),  serviceLifetime));
            }
        }

        public void RegisterSingleton(Type lookupType, object instance)
        {
            //TODO: this is needed to keep make sure dispose is called on the instance
            serviceCollection.AddSingleton(lookupType, sp => instance);
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

                container?.Dispose();
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

        void MakeSureScopeInitialized()
        {
            if (serviceScope == null)
            {
                container = serviceCollection.BuildServiceProvider(true);
                serviceScope = container.CreateScope();
            }
        }

        void ThrowIfCalledOnChildContainer()
        {
            if (serviceScope != null)
            {
                throw new InvalidOperationException("Reconfiguration of child containers is not allowed.");
            }
        }

        static IEnumerable<Type> GetAllServices(Type type)
        {
            if (type == null)
            {
                return new List<Type>();
            }

            var result = new List<Type>(type.GetInterfaces());

            foreach (var interfaceType in type.GetInterfaces())
            {
                result.AddRange(GetAllServices(interfaceType));
            }

            return result.Distinct();
        }
    }
}