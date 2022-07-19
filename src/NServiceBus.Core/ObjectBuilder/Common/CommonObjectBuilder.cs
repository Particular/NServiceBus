namespace NServiceBus
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using ObjectBuilder;
    using ObjectBuilder.Common;

    sealed class CommonObjectBuilder : IBuilder, IConfigureComponents
    {
        readonly AsyncLocal<IBuilder> currentBuilder;
        readonly IBuilder previous;

        public CommonObjectBuilder(IContainer container, AsyncLocal<IBuilder> scope = null)
        {
            this.container = container;
            currentBuilder = scope ?? new AsyncLocal<IBuilder>();
            previous = currentBuilder.Value;
            currentBuilder.Value = this;
        }

        public IBuilder CreateChildBuilder() => new CommonObjectBuilder(container.BuildChildContainer(), currentBuilder);

        public void Dispose()
        {
            //Injected at compile time
        }

        public T Build<T>() => (T)container.Build(typeof(T));

        public object Build(Type typeToBuild) => container.Build(typeToBuild);

        IEnumerable<object> IBuilder.BuildAll(Type typeToBuild) => container.BuildAll(typeToBuild);

        void IBuilder.Release(object instance) => container.Release(instance);

        public IEnumerable<T> BuildAll<T>() => container.BuildAll(typeof(T)).Cast<T>();

        public void BuildAndDispatch(Type typeToBuild, Action<object> action)
        {
            var o = container.Build(typeToBuild);
            action(o);
        }

        public void ConfigureComponent(Type concreteComponent, DependencyLifecycle instanceLifecycle) => container.Configure(concreteComponent, instanceLifecycle);

        public void ConfigureComponent<T>(DependencyLifecycle instanceLifecycle) => container.Configure(typeof(T), instanceLifecycle);

        public void ConfigureComponent<T>(Func<T> componentFactory, DependencyLifecycle instanceLifecycle) => container.Configure(componentFactory, instanceLifecycle);

        public void ConfigureComponent<T>(Func<IBuilder, T> componentFactory, DependencyLifecycle instanceLifecycle) => container.Configure(() => componentFactory(currentBuilder.Value), instanceLifecycle);

        void IConfigureComponents.RegisterSingleton(Type lookupType, object instance) => container.RegisterSingleton(lookupType, instance);

        public void RegisterSingleton<T>(T instance) => container.RegisterSingleton(typeof(T), instance);

        public bool HasComponent<T>() => container.HasComponent(typeof(T));

        public bool HasComponent(Type componentType) => container.HasComponent(componentType);

#pragma warning disable IDE0051 // Remove unused private members
        void DisposeManaged()
#pragma warning restore IDE0051 // Remove unused private members
        {
            currentBuilder.Value = previous;
            container?.Dispose();
        }

        IContainer container;
    }
}