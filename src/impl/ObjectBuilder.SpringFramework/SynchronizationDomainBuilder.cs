using System;
using System.Runtime.Remoting.Contexts;
using System.Collections.Generic;

namespace ObjectBuilder.SpringFramework
{
    [Synchronization(SynchronizationAttribute.REQUIRED)]
    public class SynchronizationDomainBuiler : ContextBoundObject, IBuilderInternal
    {
        #region IBuilderInternal Members

        public object Build(Type typeToBuild)
        {
            return helper.Build(typeToBuild);
        }

        public IEnumerable<object> BuildAll(Type typeToBuild)
        {
            return helper.BuildAll(typeToBuild);
        }

        public void BuildAndDispatch(Type typeToBuild, string methodName, params object[] methodArgs)
        {
            helper.BuildAndDispatch(typeToBuild, methodName, methodArgs);
        }

        public IComponentConfig ConfigureComponent(Type concreteComponent, ComponentCallModelEnum callModel)
        {
            return helper.ConfigureComponent(concreteComponent, callModel);
        }

        public object Configure(Type concreteComponent, ComponentCallModelEnum callModel)
        {
            return helper.Configure(concreteComponent, callModel);
        }

        #endregion

        private readonly Helper helper = new Helper();
    }
}
