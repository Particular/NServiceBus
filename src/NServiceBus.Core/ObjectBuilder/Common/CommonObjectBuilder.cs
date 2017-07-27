namespace NServiceBus
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
#if NET452
    using System.Runtime.Remoting.Messaging;
#endif
#if NETCOREAPP2_0
    using System.Threading;
#endif
    using ObjectBuilder;
    using ObjectBuilder.Common;

    class CommonObjectBuilder : IBuilder, IConfigureComponents
    {
#if NET452
        readonly string rootBuilderKey = Guid.NewGuid().ToString();
        public IBuilder CurrentBuilder
        {
            get
            {
                var logicalGetData = (IBuilder)CallContext.LogicalGetData(rootBuilderKey);
                return logicalGetData ?? this;
            }
            set
            {
                CallContext.LogicalSetData(rootBuilderKey, value);
            }
        }
#endif
#if NETCOREAPP2_0
        AsyncLocal<IBuilder> childBuilder = new AsyncLocal<IBuilder>();
        public IBuilder CurrentBuilder
        {
            get { return childBuilder.Value ?? this; }
            set { childBuilder.Value = value; }
        }
#endif
        public CommonObjectBuilder(IContainer container)
        {
            this.container = container;
        }

        public IBuilder CreateChildBuilder()
        {
            var childBuilder = new CommonObjectBuilder(container.BuildChildContainer());
            CurrentBuilder = childBuilder;
            return childBuilder;
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
            container.Configure(() => componentFactory(this.CurrentBuilder), instanceLifecycle);
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