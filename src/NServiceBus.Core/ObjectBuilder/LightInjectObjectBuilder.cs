namespace NServiceBus.ObjectBuilder
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Common;
    using Janitor;
    using LightInject;

    [SkipWeaving]
    class LightInjectObjectBuilder : IContainer
    {
        IServiceContainer container;
        Scope scope;
        bool isRootScope;

        public LightInjectObjectBuilder()
        {
            container = new ServiceContainer();
            container.ScopeManagerProvider = new PerLogicalCallContextScopeManagerProvider();
            scope = container.BeginScope();
            isRootScope = true;
        }

        public LightInjectObjectBuilder(IServiceContainer serviceContainer)
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
            container.Register(component, GetLifeTime(dependencyLifecycle));
            var interfaces = GetAllServices(component);
            foreach (var serviceType in interfaces)
            {
                container.Register(serviceType, component, GetLifeTime(dependencyLifecycle));
            }
        }

        //ILifetime GetLifeTime(DependencyLifecycle dependencyLifecycle)
        //{
        //    switch (dependencyLifecycle)
        //    {
        //        case DependencyLifecycle.InstancePerCall:
        //            return new PerRequestLifeTime();
        //        case DependencyLifecycle.InstancePerUnitOfWork:
        //            return new PerScopeLifetime();
        //        case DependencyLifecycle.SingleInstance:
        //            return new PerContainerLifetime();
        //        default:
        //            throw new Exception();
        //    }
        //}

        //TODO: taken from https://github.com/seesharper/LightInject.NServiceBus/blob/master/src/LightInject.NServiceBus/LightInject.NServiceBus.cs, they do use null instead perrequest??
        private ILifetime GetLifeTime(DependencyLifecycle dependencyLifecycle)
        {
            if (dependencyLifecycle == DependencyLifecycle.SingleInstance)
            {
                return new PerContainerLifetime();
            }
            if (dependencyLifecycle == DependencyLifecycle.InstancePerUnitOfWork)
            {
                return new PerScopeLifetime();
            }

            // TODO: is there a difference between returning null or PerRequestLifeTime?
            return null;
        }

        public void Configure<T>(Func<T> component, DependencyLifecycle dependencyLifecycle)
        {
            container.Register(sf => component(), GetLifeTime(dependencyLifecycle));
            var interfaces = GetAllServices(typeof(T));
            foreach (var serviceType in interfaces)
            {
                //TODO are those instances shared across different request types? e.g singleton resolve<Impl> & resolve<Service>
                //var serviceRegistration = new ServiceRegistration()
                //{
                //    ServiceType = serviceType,
                //    ImplementingType = typeof(T)
                //};
                //container.Register(serviceRegistration);
                container.Register(serviceType, typeof(T));

            }
        }

        public void RegisterSingleton(Type lookupType, object instance)
        {
            container.RegisterInstance(lookupType, instance);
        }

        public bool HasComponent(Type componentType)
        {
            return container.CanGetInstance(componentType, string.Empty);
        }

        public void Release(object instance)
        {
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