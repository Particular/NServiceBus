using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Spring.Context.Support;
using Spring.Objects.Factory.Support;
using Spring.Objects.Factory.Config;
using System.Reflection;
using Spring.Aop.Framework;
using Spring.Aop.Support;
using System.Collections;
using NServiceBus.ObjectBuilder.Common;
using Common.Logging;
using ObjectBuilder;

namespace NServiceBus.ObjectBuilder.Spring
{
    public class SpringObjectBuilder : IBuilderInternal
    {
        private static GenericApplicationContext context;

        public SpringObjectBuilder() : this(new GenericApplicationContext())
        {
        }

        public SpringObjectBuilder(GenericApplicationContext container)
        {
            context = container;
        }

        #region IBuilderInternal Members

        public object Build(Type typeToBuild)
        {
            this.Init();
            IDictionary dict = context.GetObjectsOfType(typeToBuild, true, false);

            if (dict.Count == 0)
                return null;

            IDictionaryEnumerator de = dict.GetEnumerator();

            return (de.MoveNext() ? de.Value : null);
        }

        public IEnumerable<object> BuildAll(Type typeToBuild)
        {
            this.Init();
            IDictionary dict = context.GetObjectsOfType(typeToBuild, true, false);

            IDictionaryEnumerator de = dict.GetEnumerator();
            while (de.MoveNext())
                yield return de.Entry.Value;
        }

        public void BuildAndDispatch(Type typeToBuild, Action<object> action)
        {
            object o = Build(typeToBuild);
            action(o);
        }

        public object Configure(Type concreteComponent, ComponentCallModelEnum callModel)
        {
            WarnAboutNonVirtualProperties(concreteComponent);

            IComponentConfig config = ConfigureComponent(concreteComponent, callModel);

            object instance = Activator.CreateInstance(concreteComponent);

            ProxyFactory pf = new ProxyFactory(instance);
            pf.AddAdvisor(new ConfigAdvisor(concreteComponent, config));
            pf.AddIntroduction(new DefaultIntroductionAdvisor(new ConfigAdvice(config)));
            pf.ProxyTargetType = true;

            return pf.GetProxy();
        }

        public IComponentConfig ConfigureComponent(Type concreteComponent, ComponentCallModelEnum callModel)
        {
            ComponentConfig result;

            lock (this.componentProperties)
            {
                componentProperties.TryGetValue(concreteComponent, out result);

                if (result == null)
                {
                    result = new ComponentConfig();
                    componentProperties[concreteComponent] = result;
                }
            }

            typeHandleLookup[concreteComponent] = callModel;

            return result;
        }

        #endregion

        private void Init()
        {
            if (initialized)
                return;

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

        private static void WarnAboutNonVirtualProperties(Type concreteComponent)
        {
            List<string> problematicProperties = new List<string>();
            foreach (PropertyInfo prop in concreteComponent.GetProperties(BindingFlags.Public | BindingFlags.FlattenHierarchy | BindingFlags.Instance))
                if (prop.GetSetMethod() != null)
                    if (!prop.GetSetMethod().IsVirtual)
                        problematicProperties.Add(prop.Name);

            if (problematicProperties.Count > 0)
                LogManager.GetLogger("ObjectBuilder").Warn(String.Format("Non virtual properties of {0} ({1}) may not be able to be configured.", concreteComponent.FullName, string.Join(", ", problematicProperties.ToArray())));
        }

        private readonly Dictionary<Type, ComponentCallModelEnum> typeHandleLookup = new Dictionary<Type, ComponentCallModelEnum>();
        private readonly Dictionary<Type, ComponentConfig> componentProperties = new Dictionary<Type, ComponentConfig>();
        private bool initialized;
        private DefaultObjectDefinitionFactory factory = new DefaultObjectDefinitionFactory();
    }
}
