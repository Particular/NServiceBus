using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ObjectBuilder;

namespace NServiceBus.ObjectBuilder.Common
{
    public interface IBuilderInternal
    {
        object Build(Type typeToBuild);

        IEnumerable<object> BuildAll(Type typeToBuild);

        void BuildAndDispatch(Type typeToBuild, Action<object> action);

        IComponentConfig ConfigureComponent(Type concreteComponent, ComponentCallModelEnum callModel);

        object Configure(Type concreteComponent, ComponentCallModelEnum callModel);
    }
}
