using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ObjectBuilder;

namespace NServiceBus.ObjectBuilder.Common
{
    public class CommonObjectBuilder : IBuilder, IConfigureComponents, IContainInternalBuilder
    {
        public virtual IBuilderInternal Builder { get; set; }

        #region IConfigureComponents Members

        public T ConfigureComponent<T>(ComponentCallModelEnum callModel)
        {
            return (T)Builder.Configure(typeof(T), callModel);
        }

        public IComponentConfig ConfigureComponent(Type concreteComponent, ComponentCallModelEnum callModel)
        {
            return Builder.ConfigureComponent(concreteComponent, callModel);
        }

        #endregion

        #region IBuilder Members

        public T Build<T>()
        {
            return (T)Build(typeof(T));
        }

        public object Build(Type typeToBuild)
        {
            return Builder.Build(typeToBuild);
        }

        public IEnumerable<object> BuildAll(Type typeToBuild)
        {
            return Builder.BuildAll(typeToBuild);
        }

        public IEnumerable<T> BuildAll<T>()
        {
            foreach(T element in BuildAll(typeof(T)))
                yield return element;
        }

        public void BuildAndDispatch(Type typeToBuild, Action<object> action)
        {
            Builder.BuildAndDispatch(typeToBuild, action);
        }

        #endregion
    }
}
