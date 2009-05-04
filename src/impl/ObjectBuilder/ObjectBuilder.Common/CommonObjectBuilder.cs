using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NServiceBus.ObjectBuilder;

namespace NServiceBus.ObjectBuilder.Common
{
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

        IComponentConfig IConfigureComponents.ConfigureComponent(Type concreteComponent, ComponentCallModelEnum callModel)
        {
            Container.Configure(concreteComponent, callModel);

            return new ComponentConfig(concreteComponent, Container);
        }

        IComponentConfig<T> IConfigureComponents.ConfigureComponent<T>(ComponentCallModelEnum callModel)
        {
            Container.Configure(typeof(T), callModel);

            return new ComponentConfig<T>(Container);
        }

        #endregion

        #region IBuilder Members

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
                action(Container.Build(typeToBuild));
        }

        #endregion

        private static SynchronizedInvoker sync;
        private IContainer container;
    }
}
