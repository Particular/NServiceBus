namespace NServiceBus
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
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
            return (T) container.Build(typeof(T));
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
            container.Configure(concreteComponent, instanceLifecycle);
        }

        public void ConfigureComponent<T>(DependencyLifecycle instanceLifecycle)
        {
            container.Configure(typeof(T), instanceLifecycle);
        }

        public void ConfigureComponent<T>(Func<T> componentFactory, DependencyLifecycle instanceLifecycle)
        {
            container.Configure(componentFactory, instanceLifecycle);
        }

        public void ConfigureComponent<T>(Func<IBuilder, T> componentFactory, DependencyLifecycle instanceLifecycle)
        {
            container.Configure(() => componentFactory(this), instanceLifecycle);
        }

        void IConfigureComponents.RegisterSingleton(Type lookupType, object instance)
        {
            container.RegisterSingleton(lookupType, instance);
        }

        public void RegisterSingleton<T>(T instance)
        {
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

        void DisposeManaged()
        {
            container?.Dispose();
        }

        IContainer container;
    }
}