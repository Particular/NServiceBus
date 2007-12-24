using System;
using System.Runtime.Remoting.Contexts;
using System.Collections;

namespace ObjectBuilder.SpringFramework
{
    [Synchronization(SynchronizationAttribute.REQUIRED)]
    public class SynchronizationDomainBuiler : ContextBoundObject, IBuilderInternal
    {
        #region IBuilderInternal Members

        public object Build(Type typeToBuild)
        {
            return Helper.Build(typeToBuild, Spring.Context.Support.ContextRegistry.GetContext());
        }

        public IEnumerable BuildAll(Type typeToBuild)
        {
            return Helper.BuildAll(typeToBuild, Spring.Context.Support.ContextRegistry.GetContext());
        }

        public void BuildAndDispatch(Type typeToBuild, string methodName, params object[] methodArgs)
        {
            Helper.BuildAndDispatch(typeToBuild, Spring.Context.Support.ContextRegistry.GetContext(), methodName, methodArgs);
        }

        #endregion
    }
}
