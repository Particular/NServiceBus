using System;
using System.Collections.Generic;

namespace ObjectBuilder
{
    public interface IBuilder
    {
        bool JoinSynchronizationDomain { set; }

        object Build(Type typeToBuild);
        T Build<T>();

        IEnumerable<T> BuildAll<T>();

        void BuildAndDispatch(Type typeToBuild, string methodName, params object[] methodArgs);
    }
}
