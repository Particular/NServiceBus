using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Remoting.Contexts;
using NServiceBus.ObjectBuilder.Common;
using ObjectBuilder;

namespace NServiceBus.ObjectBuilder.Synchronized
{
    [Synchronization(SynchronizationAttribute.REQUIRED)]
    public class SynchronizedObjectBuilder : ContextBoundObject, IBuilderInternal, IContainInternalBuilder
    {
        public IBuilderInternal Builder { get; set; }

        #region IBuilderInternal Members

        public object Build(Type typeToBuild)
        {
            return Builder.Build(typeToBuild);
        }

        public IEnumerable<object> BuildAll(Type typeToBuild)
        {
            return Builder.BuildAll(typeToBuild);
        }

        public void BuildAndDispatch(Type typeToBuild, Action<object> action)
        {
            Builder.BuildAndDispatch(typeToBuild, action);
        }

        public IComponentConfig ConfigureComponent(Type concreteComponent, ComponentCallModelEnum callModel)
        {
            return Builder.ConfigureComponent(concreteComponent, callModel);
        }

        public object Configure(Type concreteComponent, ComponentCallModelEnum callModel)
        {
            return Builder.Configure(concreteComponent, callModel);
        }

        #endregion
    }
}
