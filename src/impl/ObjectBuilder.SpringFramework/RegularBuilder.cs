using System;
using System.Collections;

namespace ObjectBuilder.SpringFramework
{
    public class RegularBuilder : IBuilderInternal
    {
        #region IBuilderInternal Members

        public object Build(Type typeToBuild)
        {
            return helper.Build(typeToBuild);
        }

        public IEnumerable BuildAll(Type typeToBuild)
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

        #endregion

        private readonly Helper helper = new Helper();
    }
}
