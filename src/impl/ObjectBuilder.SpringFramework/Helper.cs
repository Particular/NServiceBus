using System;
using System.Collections;
using System.Reflection;

namespace ObjectBuilder.SpringFramework
{
    public static class Helper
    {
        public static object Build(Type typeToBuild, Spring.Context.IApplicationContext context)
        {
            IDictionary dict = context.GetObjectsOfType(typeToBuild, true, false);

            if (dict.Count == 0)
                return null;

            IDictionaryEnumerator de = dict.GetEnumerator();

            return (de.MoveNext() ? de.Value : null);
        }

        public static IEnumerable BuildAll(Type typeToBuild, Spring.Context.IApplicationContext context)
        {
            IDictionary dict = context.GetObjectsOfType(typeToBuild, true, false);

            IDictionaryEnumerator de = dict.GetEnumerator();
            while (de.MoveNext())
                yield return de.Entry.Value;
        }

        public static void BuildAndDispatch(Type typeToBuild, Spring.Context.IApplicationContext context, string methodName, params object[] methodArgs)
        {
            Type[] types = new Type[methodArgs.Length];
            for(int i=0; i < methodArgs.Length; i++)
                types[i] = methodArgs[i].GetType();

            MethodInfo method = typeToBuild.GetMethod(methodName, types);
            object obj = Build(typeToBuild, context);

            if (obj != null)
                method.Invoke(obj, methodArgs);
        }

    }
}
