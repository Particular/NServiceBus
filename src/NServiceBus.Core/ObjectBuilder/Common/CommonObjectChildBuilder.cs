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
    class CommonObjectChildBuilder : IChildBuilder
    {
        /// <summary>
        /// The container that will be used to create objects and configure components.
        /// </summary>
        public IChildContainer Container { get; set; }

        public bool HasComponent<T>()
        {
            return Container.HasComponent(typeof(T));
        }

        public bool HasComponent(Type componentType)
        {
            return Container.HasComponent(componentType);
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

        IEnumerable<object> IChildBuilder.BuildAll(Type typeToBuild)
        {
            return Container.BuildAll(typeToBuild);
        }

        void IChildBuilder.Release(object instance)
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
