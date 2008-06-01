using System;
using System.Collections.Generic;

namespace ObjectBuilder.SpringFramework
{
    public class Builder : IBuilder
    {
        public IBuilderInternal builder = new RegularBuilder();

        #region IBuilder Members

        public bool JoinSynchronizationDomain
        {
            set
            {
                if (value)
                    this.builder = new SynchronizationDomainBuiler();
            }
        }

        public T Build<T>()
        {
            return (T)this.Build(typeof(T));
        }

        public object Build(Type typeToBuild)
        {
            return this.builder.Build(typeToBuild);
        }

        public IEnumerable<T> BuildAll<T>()
        {
            foreach(T val in this.builder.BuildAll(typeof(T)))
                yield return val;
        }

        public void BuildAndDispatch(Type typeToBuild, string methodName, params object[] methodArgs)
        {
            this.builder.BuildAndDispatch(typeToBuild, methodName, methodArgs);
        }

        public IComponentConfig ConfigureComponent(Type concreteComponent, ComponentCallModelEnum callModel)
        {
            return this.builder.ConfigureComponent(concreteComponent, callModel);
        }

        public T ConfigureComponent<T>(ComponentCallModelEnum callModel)
        {
            return (T)this.builder.Configure(typeof(T), callModel);
        }
        #endregion
    }
}
