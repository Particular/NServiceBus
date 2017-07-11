namespace NServiceBus
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Janitor;
    using LightInject;
    using ObjectBuilder;
    using ObjectBuilder.Common;

    [SkipWeaving]
    class LightInjectObjectBuilder : IContainer
    {
        IServiceContainer rootContainer;
        Scope currentScope;
        bool isDisposed = false;

        IServiceFactory serviceFactory;

        public LightInjectObjectBuilder()
        {
            rootContainer = new ServiceContainer(new ContainerOptions { EnableVariance = false });
            serviceFactory = currentScope = rootContainer.BeginScope();
        }

        LightInjectObjectBuilder(Scope childContainer)
        {
            serviceFactory = currentScope = childContainer;
        }

        LightInjectObjectBuilder(IServiceFactory serviceFactory)
        {
            this.serviceFactory = serviceFactory;
        }

        public void Dispose()
        {
            // required as the container is registered itself and we need to prevent reentrancy
            if (isDisposed)
            {
                isDisposed = true;
            }

            // do not use janitor to ensure proper order of disposal
            currentScope?.Dispose();
            rootContainer?.Dispose();
        }

        public object Build(Type typeToBuild)
        {
            return serviceFactory.GetInstance(typeToBuild);
        }

        public T Build<T>()
        {
            return serviceFactory.GetInstance<T>();
        }

        public IEnumerable<T> BuildAll<T>()
        {
            return serviceFactory.GetAllInstances<T>();
        }

        public IEnumerable<object> BuildAll(Type typeToBuild)
        {
            return serviceFactory.GetAllInstances(typeToBuild);
        }

        public IBuilder CreateChildBuilder()
        {
            // todo: NRE when called on child container (shouldn't happen though)
            return new LightInjectObjectBuilder(currentScope.BeginScope());
        }

        public void ConfigureComponent(Type concreteComponent, DependencyLifecycle dependencyLifecycle)
        {
            if (HasComponent(concreteComponent))
            {
                return;
            }

            rootContainer.Register(concreteComponent, GetLifeTime(dependencyLifecycle));

            var interfaces = GetAllServices(concreteComponent);

            foreach (var serviceType in interfaces)
            {
                rootContainer.Register(serviceType, s => s.GetInstance(concreteComponent), GetLifeTime(dependencyLifecycle), concreteComponent.FullName);
            }
        }

        public void ConfigureComponent<T>(DependencyLifecycle dependencyLifecycle)
        {
            if (HasComponent<T>())
            {
                return;
            }

            rootContainer.Register<T>(GetLifeTime(dependencyLifecycle));

            var interfaces = GetAllServices(typeof(T));

            foreach (var serviceType in interfaces)
            {
                rootContainer.Register(serviceType, s => s.GetInstance<T>(), GetLifeTime(dependencyLifecycle), typeof(T).FullName);
            }
        }

        public void ConfigureComponent<T>(Func<T> componentFactory, DependencyLifecycle dependencyLifecycle)
        {
            var componentType = typeof(T);

            if (HasComponent(componentType))
            {
                return;
            }

            rootContainer.Register(sf => componentFactory(), GetLifeTime(dependencyLifecycle));

            var interfaces = GetAllServices(componentType);

            foreach (var servicesType in interfaces)
            {
                rootContainer.Register(servicesType, s => s.GetInstance<T>(), GetLifeTime(dependencyLifecycle), componentType.FullName);
            }
        }

        public void ConfigureComponent<T>(Func<IBuilder, T> componentFactory, DependencyLifecycle dependencyLifecycle)
        {
            var componentType = typeof(T);

            if (HasComponent(componentType))
            {
                return;
            }

            rootContainer.Register(sf => componentFactory(new LightInjectObjectBuilder(sf)), GetLifeTime(dependencyLifecycle));

            var interfaces = GetAllServices(componentType);

            foreach (var servicesType in interfaces)
            {
                rootContainer.Register(servicesType, s => s.GetInstance<T>(), GetLifeTime(dependencyLifecycle), componentType.FullName);
            }
        }

        public void RegisterSingleton(Type lookupType, object instance)
        {
            rootContainer.RegisterInstance(lookupType, instance);
        }

        public void RegisterSingleton<T>(T instance)
        {
            rootContainer.RegisterInstance(instance);
        }

        public bool HasComponent<T>()
        {
            return rootContainer.CanGetInstance(typeof(T), string.Empty);
        }

        public bool HasComponent(Type componentType)
        {
            return rootContainer.CanGetInstance(componentType, string.Empty);
        }

        static ILifetime GetLifeTime(DependencyLifecycle dependencyLifecycle)
        {
            switch (dependencyLifecycle)
            {
                case DependencyLifecycle.InstancePerCall:
                    return new PerRequestLifeTime();
                case DependencyLifecycle.InstancePerUnitOfWork:
                    return new PerScopeLifetime();
                case DependencyLifecycle.SingleInstance:
                    return new PerContainerLifetime();
                default:
                    throw new Exception();
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