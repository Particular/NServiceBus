using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Remoting.Contexts;
using NServiceBus.ObjectBuilder.Common;
using NServiceBus.ObjectBuilder;

namespace NServiceBus.ObjectBuilder.Synchronized
{
    /// <summary>
    /// Implementation of IBuilderInternal for smart clients.
    /// Inherits from ContextBoundObject and via the SynchronizationAttribute
    /// provides thread-safety facilities.
    /// </summary>
    [Synchronization(SynchronizationAttribute.REQUIRED)]
    public class SynchronizedObjectBuilder : ContextBoundObject, IBuilderInternal, IContainInternalBuilder
    {
        /// <summary>
        /// The builder used to actually provide builder services.
        /// </summary>
        public IBuilderInternal Builder { get; set; }

        #region IBuilderInternal Members

        /// <summary>
        /// Dispatched to the Builder
        /// </summary>
        /// <param name="typeToBuild"></param>
        /// <returns></returns>
        public object Build(Type typeToBuild)
        {
            return Builder.Build(typeToBuild);
        }

        /// <summary>
        /// Dispatched to the Builder
        /// </summary>
        /// <param name="typeToBuild"></param>
        /// <returns></returns>
        public IEnumerable<object> BuildAll(Type typeToBuild)
        {
            return Builder.BuildAll(typeToBuild);
        }

        /// <summary>
        /// Dispatched to the Builder
        /// </summary>
        /// <param name="typeToBuild"></param>
        /// <param name="action"></param>
        public void BuildAndDispatch(Type typeToBuild, Action<object> action)
        {
            Builder.BuildAndDispatch(typeToBuild, action);
        }

        /// <summary>
        /// Dispatched to the Builder
        /// </summary>
        /// <param name="concreteComponent"></param>
        /// <param name="callModel"></param>
        /// <returns></returns>
        public IComponentConfig ConfigureComponent(Type concreteComponent, ComponentCallModelEnum callModel)
        {
            return Builder.ConfigureComponent(concreteComponent, callModel);
        }

        /// <summary>
        /// Dispatched to the Builder
        /// </summary>
        /// <param name="concreteComponent"></param>
        /// <param name="callModel"></param>
        /// <returns></returns>
        public object Configure(Type concreteComponent, ComponentCallModelEnum callModel)
        {
            return Builder.Configure(concreteComponent, callModel);
        }

        #endregion
    }
}
