using System;
using System.Collections;

namespace ObjectBuilder.SpringFramework
{
    public interface IBuilderInternal
    {
        object Build(Type typeToBuild);

        IEnumerable BuildAll(Type typeToBuild);

        void BuildAndDispatch(Type typeToBuild, string methodName, params object[] methodArgs);

        IComponentConfig ConfigureComponent(Type concreteComponent, ComponentCallModelEnum callModel);
    }
}
