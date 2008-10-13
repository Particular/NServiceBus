using System;
using System.Collections;
using System.Reflection;
using Spring.Aop;
using Spring.Aop.Support;
using Spring.Context.Support;
using Spring.Objects.Factory.Support;
using Spring.Objects.Factory.Config;
using System.Collections.Generic;
using AopAlliance.Intercept;
using Spring.Aop.Framework;

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

            this.ConfigureComponent(typeof(Builder), ComponentCallModelEnum.Singleton);

            lock (this.componentProperties)
            {
                foreach (Type t in this.componentProperties.Keys)
                {
                    ObjectDefinitionBuilder builder = ObjectDefinitionBuilder.RootObjectDefinition(factory, t)
                        .SetAutowireMode(AutoWiringMode.AutoDetect)
                        .SetSingleton(typeHandleLookup[t] == ComponentCallModelEnum.Singleton);

                    componentProperties[t].Configure(builder);

                    IObjectDefinition def = builder.ObjectDefinition;
                    context.RegisterObjectDefinition(t.FullName, def);
                }
            }

            this.initialized = true;
        }

        #endregion

        #region public methods

        public IComponentConfig ConfigureComponent(Type concreteComponent, ComponentCallModelEnum callModel)
        {
            ComponentConfig result = new ComponentConfig();

            lock(this.componentProperties)
                componentProperties[concreteComponent] = result;

            typeHandleLookup[concreteComponent] = callModel;

            return result;
        }

        public object Configure(Type concreteComponent, ComponentCallModelEnum callModel)
        {
            WarnAboutNonVirtualProperties(concreteComponent);

            ComponentConfig config = new ComponentConfig();

            lock(this.componentProperties)
                componentProperties[concreteComponent] = config;

            typeHandleLookup[concreteComponent] = callModel;

            object instance = Activator.CreateInstance(concreteComponent);

            ProxyFactory pf = new ProxyFactory(instance);
            pf.AddAdvisor(new ConfigAdvisor(concreteComponent, config));
            pf.AddIntroduction(new DefaultIntroductionAdvisor(new ConfigAdvice(config)));
            pf.ProxyTargetType = true;

            return pf.GetProxy();
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

        private static void WarnAboutNonVirtualProperties(Type concreteComponent)
        {
            List<string> problematicProperties = new List<string>();
            foreach (PropertyInfo prop in concreteComponent.GetProperties(BindingFlags.Public | BindingFlags.FlattenHierarchy | BindingFlags.Instance))
                if (prop.GetSetMethod() != null)
                    if (!prop.GetSetMethod().IsVirtual)
                        problematicProperties.Add(prop.Name);

            if (problematicProperties.Count > 0)
                Common.Logging.LogManager.GetLogger(typeof(Builder)).Warn(String.Format("Non virtual properties of {0} ({1}) may not be able to be configured.", concreteComponent.FullName, string.Join(", ", problematicProperties.ToArray())));
        }
    }

    public class ConfigAdvisor : DefaultPointcutAdvisor
    {
        public ConfigAdvisor(Type type, IComponentConfig config)
            : base(new SetterPointcut(type), new ConfigAdvice(config))
        { }

        private class SetterPointcut : IPointcut
        {
            private IMethodMatcher methodMatcher = TrueMethodMatcher.True;
            private ITypeFilter typeFilter;

            public SetterPointcut(Type type)
            {
                typeFilter = new RootTypeFilter(type);
            }

            public ITypeFilter TypeFilter
            {
                get { return typeFilter; }
            }

            public IMethodMatcher MethodMatcher
            {
                get { return methodMatcher; }
            }
        }
    }

    public class ConfigAdvice : IMethodInterceptor
    {
        private readonly IComponentConfig config;

        public ConfigAdvice(IComponentConfig config)
        {
            this.config = config;
        }

        public virtual Object Invoke(IMethodInvocation invocation)
        {
            MethodInfo method = invocation.Method;

            if (IsSetter(method))
            {
                string name = GetName(invocation.Method.Name);
                config.ConfigureProperty(name, invocation.Arguments[0]);

                string message = method.DeclaringType.Name + "." + name + " = ";
                if (invocation.Arguments[0] is IEnumerable && !(invocation.Arguments[0] is string))
                {
                    message += "{";

                    foreach (object o in (IEnumerable)invocation.Arguments[0])
                        if (o is DictionaryEntry)
                            message += "<" + ((DictionaryEntry)o).Key + ", " + ((DictionaryEntry)o).Value + ">, ";
                        else
                            message += o + ", ";

                    message += "}";
                }
                else message += invocation.Arguments[0];

                Common.Logging.LogManager.GetLogger(typeof(IBuilder)).Debug(message);
            }

            return null;
        }

        private static bool IsSetter(MethodInfo method)
        {
            return (method.Name.StartsWith("set_")) && (method.GetParameters().Length == 1);
        }

        private static string GetName(string setterName)
        {
            return setterName.Replace("set_","");
        }
    }
}
