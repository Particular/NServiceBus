namespace NServiceBus
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Linq.Expressions;
    using NServiceBus.ObjectBuilder;
    using NServiceBus.ObjectBuilder.Common;

    /// <summary>
    /// Implementation of IBuilder, serving as a facade that container specific implementations
    /// of IContainer should run behind.
    /// </summary>
    class CommonObjectBuilder : IBuilder, IConfigureComponents
    {
        /// <summary>
        /// The container that will be used to create objects and configure components.
        /// </summary>
        public IContainer Container { get; set; }

        public IComponentConfig ConfigureComponent(Type concreteComponent, DependencyLifecycle instanceLifecycle)
        {
            Container.Configure(concreteComponent, instanceLifecycle);

            return new ComponentConfig(concreteComponent, Container);
        }

        public IComponentConfig<T> ConfigureComponent<T>(DependencyLifecycle instanceLifecycle)
        {
            Container.Configure(typeof(T), instanceLifecycle);

            return new ComponentConfig<T>(Container);
        }

        public IComponentConfig<T> ConfigureComponent<T>(Func<T> componentFactory, DependencyLifecycle instanceLifecycle)
        {
            Container.Configure(componentFactory, instanceLifecycle);

            return new ComponentConfig<T>(Container);
        }

        public IComponentConfig<T> ConfigureComponent<T>(Func<IBuilder,T> componentFactory, DependencyLifecycle instanceLifecycle)
        {
            Container.Configure(() => componentFactory(this), instanceLifecycle);

            return new ComponentConfig<T>(Container);
        }

        public IConfigureComponents ConfigureProperty<T>(Expression<Func<T, object>> property, object value)
        {
            var prop = Reflect<T>.GetProperty(property);
            
            return ((IConfigureComponents)this).ConfigureProperty<T>(prop.Name,value);
        }

        public IConfigureComponents ConfigureProperty<T>(string propertyName, object value)
        {
            Container.ConfigureProperty(typeof(T), propertyName, value);

            return this;
        }

        IConfigureComponents IConfigureComponents.RegisterSingleton(Type lookupType, object instance)
        {
            Container.RegisterSingleton(lookupType, instance);
            return this;
        }

        public IConfigureComponents RegisterSingleton<T>(T instance)
        {
            Container.RegisterSingleton(typeof(T), instance);
            return this;
        }

        public bool HasComponent<T>()
        {
            return Container.HasComponent(typeof(T));
        }

        public bool HasComponent(Type componentType)
        {
            return Container.HasComponent(componentType);
        }


        public IBuilder CreateChildBuilder()
        {
            return new CommonObjectBuilder
            {
                Container = Container.BuildChildContainer()
            };
        }

        public void Dispose()
        {
            //Injected at compile time
        }

        void DisposeManaged()
        {
            Container?.Dispose();
        }

        public T Build<T>()
        {
            return (T)Container.Build(typeof(T));
        }

        public object Build(Type typeToBuild)
        {
            return Container.Build(typeToBuild);
        }

        IEnumerable<object> IBuilder.BuildAll(Type typeToBuild)
        {
            return Container.BuildAll(typeToBuild);
        }

        void IBuilder.Release(object instance)
        {
            Container.Release(instance);
        }

        public IEnumerable<T> BuildAll<T>()
        {
            return Container.BuildAll(typeof(T)).Cast<T>();
        }

        public void BuildAndDispatch(Type typeToBuild, Action<object> action)
        {
            var o = Container.Build(typeToBuild);
            action(o);
        }
    }
}
