using System;
using System.Collections.Generic;

namespace ObjectBuilder.SpringFramework
{
    public interface IBuilderInternal
    {
        object Build(Type typeToBuild);

        IEnumerable<object> BuildAll(Type typeToBuild);

        void BuildAndDispatch(Type typeToBuild, string methodName, params object[] methodArgs);

        IComponentConfig ConfigureComponent(Type concreteComponent, ComponentCallModelEnum callModel);

        object Configure(Type concreteComponent, ComponentCallModelEnum callModel);
    }
}
