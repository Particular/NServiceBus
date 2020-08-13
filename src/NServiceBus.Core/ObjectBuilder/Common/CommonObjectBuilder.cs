namespace NServiceBus
{
    using System;
    using System.Collections.Generic;
    using ObjectBuilder;
    using ObjectBuilder.Common;

    class CommonObjectBuilder : IBuilder, IConfigureComponents
    {
        public CommonObjectBuilder(IContainer container)
        {
            this.container = container;
        }

        public object GetService(Type serviceType)
        {
            return container.Build(serviceType);
        }

        public IBuilder CreateChildBuilder()
        {
            throw new NotSupportedException();
        }

        public void Dispose()
        {
            //Injected at compile time
        }

        public T Build<T>()
        {
            throw new NotSupportedException();
        }

        public object Build(Type typeToBuild)
        {
            throw new NotSupportedException();
        }

        IEnumerable<object> IBuilder.BuildAll(Type typeToBuild)
        {
            throw new NotSupportedException();
        }

        void IBuilder.Release(object instance)
        {
            throw new NotSupportedException();
        }

        public IEnumerable<T> BuildAll<T>()
        {
            throw new NotSupportedException();
        }

        public void BuildAndDispatch(Type typeToBuild, Action<object> action)
        {
            throw new NotSupportedException();
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