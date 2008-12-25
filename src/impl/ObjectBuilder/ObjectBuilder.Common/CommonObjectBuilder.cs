using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ObjectBuilder;

namespace NServiceBus.ObjectBuilder.Common
{
    /// <summary>
    /// Implementation of IBuilder, serving as a facade that container specific implementations
    /// of IBuilderInternal should run behind.
    /// </summary>
    public class CommonObjectBuilder : IBuilder, IConfigureComponents, IContainInternalBuilder
    {
        /// <summary>
        /// Injected container-specific builder.
        /// </summary>
        public virtual IBuilderInternal Builder { get; set; }

        #region IConfigureComponents Members

        /// <summary>
        /// Forwards the call to the injected builder.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="callModel"></param>
        /// <returns></returns>
        public T ConfigureComponent<T>(ComponentCallModelEnum callModel)
        {
            return (T)Builder.Configure(typeof(T), callModel);
        }

        /// <summary>
        /// Forwards the call to the injected builder.
        /// </summary>
        /// <param name="concreteComponent"></param>
        /// <param name="callModel"></param>
        /// <returns></returns>
        public IComponentConfig ConfigureComponent(Type concreteComponent, ComponentCallModelEnum callModel)
        {
            return Builder.ConfigureComponent(concreteComponent, callModel);
        }

        #endregion

        #region IBuilder Members

        /// <summary>
        /// Forwards the call to the injected builder.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public T Build<T>()
        {
            return (T)Build(typeof(T));
        }

        /// <summary>
        /// Forwards the call to the injected builder.
        /// </summary>
        /// <param name="typeToBuild"></param>
        /// <returns></returns>
        public object Build(Type typeToBuild)
        {
            return Builder.Build(typeToBuild);
        }

        /// <summary>
        /// Forwards the call to the injected builder.
        /// </summary>
        /// <param name="typeToBuild"></param>
        /// <returns></returns>
        public IEnumerable<object> BuildAll(Type typeToBuild)
        {
            return Builder.BuildAll(typeToBuild);
        }

        /// <summary>
        /// Forwards the call to the injected builder.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public IEnumerable<T> BuildAll<T>()
        {
            foreach(T element in BuildAll(typeof(T)))
                yield return element;
        }

        /// <summary>
        /// Forwards the call to the injected builder.
        /// </summary>
        /// <param name="typeToBuild"></param>
        /// <param name="action"></param>
        public void BuildAndDispatch(Type typeToBuild, Action<object> action)
        {
            Builder.BuildAndDispatch(typeToBuild, action);
        }

        #endregion
    }
}
