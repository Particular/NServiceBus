namespace NServiceBus
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using MessageMutator;
    using ObjectBuilder;
    using ObjectBuilder.Common;

    class CommonObjectBuilder : IBuilder, IConfigureComponents
    {
        public CommonObjectBuilder(IContainer container)
        {
            this.container = container;
        }

        public IBuilder CreateChildBuilder()
        {
            return new CommonObjectBuilder(container.BuildChildContainer());
        }

        public void Dispose()
        {
            //Injected at compile time
        }

        public T Build<T>()
        {
            return (T)container.Build(typeof(T));
        }

        public object Build(Type typeToBuild)
        {
            return container.Build(typeToBuild);
        }

        IEnumerable<object> IBuilder.BuildAll(Type typeToBuild)
        {
            return container.BuildAll(typeToBuild);
        }

        void IBuilder.Release(object instance)
        {
            container.Release(instance);
        }

        public IEnumerable<T> BuildAll<T>()
        {
            return container.BuildAll(typeof(T)).Cast<T>();
        }

        public void BuildAndDispatch(Type typeToBuild, Action<object> action)
        {
            var o = container.Build(typeToBuild);
            action(o);
        }

        public void ConfigureComponent(Type concreteComponent, DependencyLifecycle instanceLifecycle)
        {
            GuardAgainstInvalidRegistrations(concreteComponent);

            container.Configure(concreteComponent, instanceLifecycle);
        }

        public void ConfigureComponent<T>(DependencyLifecycle instanceLifecycle)
        {
            GuardAgainstInvalidRegistrations(typeof(T));

            container.Configure(typeof(T), instanceLifecycle);
        }

        public void ConfigureComponent<T>(Func<T> componentFactory, DependencyLifecycle instanceLifecycle)
        {
            GuardAgainstInvalidRegistrations(typeof(T));

            container.Configure(componentFactory, instanceLifecycle);
        }

        public void ConfigureComponent<T>(Func<IBuilder, T> componentFactory, DependencyLifecycle instanceLifecycle)
        {
            GuardAgainstInvalidRegistrations(typeof(T));

            container.Configure(() => componentFactory(this), instanceLifecycle);
        }

        void IConfigureComponents.RegisterSingleton(Type lookupType, object instance)
        {
            GuardAgainstInvalidRegistrations(lookupType);

            container.RegisterSingleton(lookupType, instance);
        }

        public void RegisterSingleton<T>(T instance)
        {
            GuardAgainstInvalidRegistrations(typeof(T));

            container.RegisterSingleton(typeof(T), instance);
        }

        public bool HasComponent<T>()
        {
            return container.HasComponent(typeof(T));
        }

        public bool HasComponent(Type componentType)
        {
            return container.HasComponent(componentType);
        }

        static void GuardAgainstInvalidRegistrations(Type component)
        {
            if (typeof(IMutateIncomingMessages).IsAssignableFrom(component) ||
                typeof(IMutateOutgoingMessages).IsAssignableFrom(component) ||
                typeof(IMutateIncomingTransportMessages).IsAssignableFrom(component) ||
                typeof(IMutateOutgoingTransportMessages).IsAssignableFrom(component))
            {
                throw new Exception("Registering message mutators in the container is no longer supported. Please us `EndpointConfiguration.RegisterMessageMutator(...)`");
            }
        }

        void DisposeManaged()
        {
            container?.Dispose();
        }

        IContainer container;
    }
}