namespace NServiceBus.ObjectBuilder.Common
{
    using System;
    using System.Collections.Generic;
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

        private bool synchronized;

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

                    sync.Container = Container;
                }
            }
        }

        #region IConfigureComponents Members
        IComponentConfig IConfigureComponents.ConfigureComponent(Type concreteComponent, DependencyLifecycle instanceLifecycle)
        {
            Container.Configure(concreteComponent, instanceLifecycle);

            return new ComponentConfig(concreteComponent, Container);
        }

        IComponentConfig<T> IConfigureComponents.ConfigureComponent<T>(DependencyLifecycle instanceLifecycle)
        {
            Container.Configure(typeof(T), instanceLifecycle);

            return new ComponentConfig<T>(Container);
        }

        IComponentConfig<T> IConfigureComponents.ConfigureComponent<T>(Func<T> componentFactory, DependencyLifecycle instanceLifecycle)
        {
            Container.Configure(componentFactory, instanceLifecycle);

            return new ComponentConfig<T>(Container);
        }

        IComponentConfig<T> IConfigureComponents.ConfigureComponent<T>(Func<IBuilder,T> componentFactory, DependencyLifecycle instanceLifecycle)
        {
            Container.Configure(() => componentFactory(this), instanceLifecycle);

            return new ComponentConfig<T>(Container);
        }

        IComponentConfig IConfigureComponents.ConfigureComponent(Type concreteComponent, ComponentCallModelEnum callModel)
        {
            var lifecycle = MapToDependencyLifecycle(callModel);

            Container.Configure(concreteComponent, lifecycle);

            return new ComponentConfig(concreteComponent, Container);
        }


        IComponentConfig<T> IConfigureComponents.ConfigureComponent<T>(ComponentCallModelEnum callModel)
        {
            var lifecycle = MapToDependencyLifecycle(callModel);
            
            Container.Configure(typeof(T), lifecycle);

            return new ComponentConfig<T>(Container);
        }

        IConfigureComponents IConfigureComponents.ConfigureProperty<T>(Expression<Func<T, object>> property, object value)
        {
            var prop = Reflect<T>.GetProperty(property);
            
            return ((IConfigureComponents)this).ConfigureProperty<T>(prop.Name,value);
        }

        IConfigureComponents IConfigureComponents.ConfigureProperty<T>(string propertyName, object value)
        {
            Container.ConfigureProperty(typeof(T), propertyName, value);

            return this;
        }

        IConfigureComponents IConfigureComponents.RegisterSingleton(Type lookupType, object instance)
        {
            Container.RegisterSingleton(lookupType, instance);
            return this;
        }

        IConfigureComponents IConfigureComponents.RegisterSingleton<T>(object instance)
        {
            Container.RegisterSingleton(typeof(T), instance);
            return this;
        }

        bool IConfigureComponents.HasComponent<T>()
        {
            return Container.HasComponent(typeof(T));
        }

        bool IConfigureComponents.HasComponent(Type componentType)
        {
            return Container.HasComponent(componentType);
        }

        #endregion

        #region IBuilder Members

        IBuilder IBuilder.CreateChildBuilder()
        {
            return new CommonObjectBuilder
            {
                Container = Container.BuildChildContainer()
            };
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposed)
            {
                return;
            }

            if (disposing)
            {
                // Dispose managed resources.
                Container.Dispose();
            }

            disposed = true;
        }

        ~CommonObjectBuilder()
        {
            Dispose(false);
        }

        T IBuilder.Build<T>()
        {
            return (T)Container.Build(typeof(T));
        }

        object IBuilder.Build(Type typeToBuild)
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

        IEnumerable<T> IBuilder.BuildAll<T>()
        {
            foreach (T element in Container.BuildAll(typeof(T)))
                yield return element;
        }

        void IBuilder.BuildAndDispatch(Type typeToBuild, Action<object> action)
        {
            if (sync != null)
                sync.BuildAndDispatch(typeToBuild, action);
            else
            {
                var o = Container.Build(typeToBuild);
                action(o);
            }
        }

        #endregion

        static DependencyLifecycle MapToDependencyLifecycle(ComponentCallModelEnum callModel)
        {
            switch (callModel)
            {
                case ComponentCallModelEnum.Singleton:
                    return DependencyLifecycle.SingleInstance;
                case ComponentCallModelEnum.Singlecall:
                    return DependencyLifecycle.InstancePerCall;
                case ComponentCallModelEnum.None:
                    return DependencyLifecycle.InstancePerUnitOfWork;
            }

            throw new ArgumentException("Unhandled component call model: " + callModel);
        }

        private bool disposed;
        private static SynchronizedInvoker sync;
        private IContainer container;
    }
}
