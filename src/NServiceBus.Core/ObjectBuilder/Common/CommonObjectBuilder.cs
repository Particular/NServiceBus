namespace NServiceBus.ObjectBuilder.Common
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Linq.Expressions;
    using Utils.Reflection;

    /// <summary>
    /// Implementation of IBuilder, serving as a facade that container specific implementations
    /// of IContainer should run behind.
    /// </summary>
    public class CommonObjectBuilder : IBuilder, IConfigureComponents
    {
        /// <summary>
        /// The container that will be used to create objects and configure components.
        /// </summary>
        public IContainer Container
        {
            get { return container; }
            set
            {
                container = value;
                if (sync != null)
                    sync.Container = value;
            }
        }

        bool synchronized;

        /// <summary>
        /// Used for multi-threaded rich clients to build and dispatch
        /// in a synchronization domain.
        /// </summary>
        public bool Synchronized
        {
            get { return synchronized; }
            set
            {
                synchronized = value;

                if (synchronized)
                {
                    if (sync == null)
                        sync = new SynchronizedInvoker();

                    sync.Container = container;
                }
            }
        }

        public IComponentConfig ConfigureComponent(Type concreteComponent, DependencyLifecycle instanceLifecycle)
        {
            container.Configure(concreteComponent, instanceLifecycle);

            return new ComponentConfig(concreteComponent, container);
        }

        public IComponentConfig<T> ConfigureComponent<T>(DependencyLifecycle instanceLifecycle)
        {
            container.Configure(typeof(T), instanceLifecycle);

            return new ComponentConfig<T>(container);
        }

        public IComponentConfig<T> ConfigureComponent<T>(Func<T> componentFactory, DependencyLifecycle instanceLifecycle)
        {
            container.Configure(componentFactory, instanceLifecycle);

            return new ComponentConfig<T>(container);
        }

        public IComponentConfig<T> ConfigureComponent<T>(Func<IBuilder,T> componentFactory, DependencyLifecycle instanceLifecycle)
        {
            container.Configure(() => componentFactory(this), instanceLifecycle);

            return new ComponentConfig<T>(container);
        }

        public IConfigureComponents ConfigureProperty<T>(Expression<Func<T, object>> property, object value)
        {
            var prop = Reflect<T>.GetProperty(property);
            
            return ((IConfigureComponents)this).ConfigureProperty<T>(prop.Name,value);
        }

        public IConfigureComponents ConfigureProperty<T>(string propertyName, object value)
        {
            container.ConfigureProperty(typeof(T), propertyName, value);

            return this;
        }

        IConfigureComponents IConfigureComponents.RegisterSingleton(Type lookupType, object instance)
        {
            container.RegisterSingleton(lookupType, instance);
            return this;
        }

        public IConfigureComponents RegisterSingleton<T>(object instance)
        {
            container.RegisterSingleton(typeof(T), instance);
            return this;
        }

        public bool HasComponent<T>()
        {
            return container.HasComponent(typeof(T));
        }

        public bool HasComponent(Type componentType)
        {
            return container.HasComponent(componentType);
        }


        public IBuilder CreateChildBuilder()
        {
            return new CommonObjectBuilder
            {
                Container = container.BuildChildContainer()
            };
        }

        public void Dispose()
        {
            //Injected at compile time
        }

        void DisposeManaged()
        {
            if (container != null)
            {
                container.Dispose();
            }
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
            if (sync != null)
                sync.BuildAndDispatch(typeToBuild, action);
            else
            {
                var o = container.Build(typeToBuild);
                action(o);
            }
        }

        static SynchronizedInvoker sync;
        IContainer container;
    }
}
