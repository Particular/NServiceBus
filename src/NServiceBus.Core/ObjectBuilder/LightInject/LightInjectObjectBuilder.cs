namespace NServiceBus
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Janitor;
    using LightInject;
    using ObjectBuilder.Common;

    [SkipWeaving]
    class LightInjectObjectBuilder : IContainer
    {
        public LightInjectObjectBuilder()
        {
            container = new ServiceContainer(new ContainerOptions
            {
                EnableVariance = false
            })
            {
                // Logical call context is necessary because the CurrentScope would be managed in a thread local 
                // by default, if not specified otherwise, which leads to inproper scope 
                // usage when executed with async code.
                ScopeManagerProvider = new PerLogicalCallContextScopeManagerProvider()
            };
            scope = container.BeginScope();
            isRootScope = true;
        }

        LightInjectObjectBuilder(IServiceContainer serviceContainer)
        {
            container = serviceContainer;
            scope = serviceContainer.BeginScope();
            isRootScope = false;
        }

        public void Dispose()
        {
            scope.Dispose();

            if (isRootScope)
            {
                container.Dispose();
            }
        }

        public object Build(Type typeToBuild)
        {
            return scope?.GetInstance(typeToBuild);
        }

        public IContainer BuildChildContainer()
        {
            return new LightInjectObjectBuilder(container);
        }

        public IEnumerable<object> BuildAll(Type typeToBuild)
        {
            return scope?.GetAllInstances(typeToBuild);
        }

        public void Configure(Type component, DependencyLifecycle dependencyLifecycle)
        {
            ThrowIfCalledOnChildContainer();

            if (HasComponent(component))
            {
                return;
            }

            container.Register(component, GetLifeTime(dependencyLifecycle));

            var interfaces = GetAllServices(component);

            foreach (var serviceType in interfaces)
            {
                container.Register(serviceType, s => s.GetInstance(component), GetLifeTime(dependencyLifecycle), component.FullName);
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

            container.Register(sf => component(), GetLifeTime(dependencyLifecycle));

            var interfaces = GetAllServices(componentType);

            foreach (var servicesType in interfaces)
            {
                container.Register(servicesType, s => s.GetInstance<T>(), GetLifeTime(dependencyLifecycle), componentType.FullName);
            }
        }

        public void RegisterSingleton(Type lookupType, object instance)
        {
            ThrowIfCalledOnChildContainer();

            container.RegisterInstance(lookupType, instance);
        }

        public bool HasComponent(Type componentType)
        {
            return container.CanGetInstance(componentType, string.Empty);
        }

        public void Release(object instance)
        {
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

        void ThrowIfCalledOnChildContainer()
        {
            if (!isRootScope)
            {
                throw new InvalidOperationException("Reconfiguration of child containers is not allowed.");
            }
        }

        IServiceContainer container;
        Scope scope;
        bool isRootScope;
    }
}