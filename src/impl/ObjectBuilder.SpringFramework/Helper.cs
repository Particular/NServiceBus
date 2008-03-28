using System;
using System.Collections;
using System.Reflection;
using Spring.Context.Support;
using Spring.Objects.Factory.Support;
using Spring.Objects.Factory.Config;
using System.Collections.Generic;

namespace ObjectBuilder.SpringFramework
{
    public class Helper
    {
        private static GenericApplicationContext context = new GenericApplicationContext();
        private readonly Dictionary<Type, ComponentCallModelEnum> typeHandleLookup = new Dictionary<Type, ComponentCallModelEnum>();
        private readonly Dictionary<Type, ComponentConfig> componentProperties = new Dictionary<Type, ComponentConfig>();
        private bool initialized;
        private DefaultObjectDefinitionFactory factory = new DefaultObjectDefinitionFactory();

        #region private methods

        private void Init()
        {
            if (initialized)
                return;

            this.ConfigureComponent(typeof (Builder), ComponentCallModelEnum.Singleton);

            foreach(Type t in this.componentProperties.Keys)
            {
                ObjectDefinitionBuilder builder = ObjectDefinitionBuilder.RootObjectDefinition(factory, t)
                    .SetAutowireMode(AutoWiringMode.AutoDetect)
                    .SetSingleton(typeHandleLookup[t] == ComponentCallModelEnum.Singleton);

                componentProperties[t].Configure(builder);

                IObjectDefinition def = builder.ObjectDefinition;
                context.RegisterObjectDefinition(t.FullName, def);
            }
        }

        #endregion

        #region public methods

        public IComponentConfig ConfigureComponent(Type concreteComponent, ComponentCallModelEnum callModel)
        {
            ComponentConfig result = new ComponentConfig();

            componentProperties[concreteComponent] = result;
            typeHandleLookup[concreteComponent] = callModel;

            return result;
        }

        public object Build(Type typeToBuild)
        {
            this.Init();
            IDictionary dict = context.GetObjectsOfType(typeToBuild, true, false);

            if (dict.Count == 0)
                return null;

            IDictionaryEnumerator de = dict.GetEnumerator();

            return (de.MoveNext() ? de.Value : null);
        }

        public IEnumerable BuildAll(Type typeToBuild)
        {
            this.Init();
            IDictionary dict = context.GetObjectsOfType(typeToBuild, true, false);

            IDictionaryEnumerator de = dict.GetEnumerator();
            while (de.MoveNext())
                yield return de.Entry.Value;
        }

        public void BuildAndDispatch(Type typeToBuild, string methodName, params object[] methodArgs)
        {
            Type[] types = new Type[methodArgs.Length];
            for(int i=0; i < methodArgs.Length; i++)
                types[i] = methodArgs[i].GetType();

            MethodInfo method = typeToBuild.GetMethod(methodName, types);
            object obj = Build(typeToBuild);

            if (obj != null)
                method.Invoke(obj, methodArgs);
        }

        #endregion
    }
}
