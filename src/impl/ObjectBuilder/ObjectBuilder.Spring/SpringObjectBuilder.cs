using System;
using System.Collections.Generic;
using Spring.Context.Support;
using Spring.Objects.Factory.Support;
using Spring.Objects.Factory.Config;
using System.Collections;
using NServiceBus.ObjectBuilder.Common;
using Spring.Context;

namespace NServiceBus.ObjectBuilder.Spring
{
    /// <summary>
    /// Implementation of IBuilderInternal using the Spring Framework container
    /// </summary>
    public class SpringObjectBuilder : IContainer
    {
        private static GenericApplicationContext context;

        /// <summary>
        /// Instantiates the builder using a new GenericApplicationContext.
        /// </summary>
        public SpringObjectBuilder() : this(new GenericApplicationContext())
        {
        }

        /// <summary>
        /// Instantiates the builder using the given container.
        /// </summary>
        /// <param name="container"></param>
        public SpringObjectBuilder(GenericApplicationContext container)
        {
            context = container;
        }

        /// <summary>
        /// No resources to dispose.
        /// </summary>
        public void Dispose()
        {
            // no-op
        }

        /// <summary>
        /// Returns the current instance.
        /// </summary>
        /// <returns></returns>
        public IContainer BuildChildContainer()
        {
            return this; // no-op
        }

        object IContainer.Build(Type typeToBuild)
        {
            Init();
            IDictionary dict = context.GetObjectsOfType(typeToBuild, true, false);

            var de = dict.GetEnumerator();

            if (de.MoveNext())
                return de.Value;
            
            throw new ArgumentException(string.Format("{0} has not been configured. In order to avoid this exception, check the return value of the 'HasComponent' method for this type.", typeToBuild));
        }

        IEnumerable<object> IContainer.BuildAll(Type typeToBuild)
        {
            Init();
            IDictionary dict = context.GetObjectsOfType(typeToBuild, true, false);

            IDictionaryEnumerator de = dict.GetEnumerator();
            while (de.MoveNext())
                yield return de.Entry.Value;
        }

        void IContainer.ReleaseInstance(object instance)
        {
            var d = instance as IDisposable;
            if (d != null)
                d.Dispose();
        }

        void IContainer.Configure(Type concreteComponent, ComponentCallModelEnum callModel)
        {
            typeHandleLookup[concreteComponent] = callModel;

            lock (componentProperties)
                if (!componentProperties.ContainsKey(concreteComponent))
                    componentProperties[concreteComponent] = new ComponentConfig();
        }

        void IContainer.ConfigureProperty(Type concreteComponent, string property, object value)
        {
            lock (componentProperties)
            {
                ComponentConfig result;
                componentProperties.TryGetValue(concreteComponent, out result);

                if (result == null)
                    throw new InvalidOperationException("Cannot configure property before the component has been configured. Please call 'Configure' first.");

                result.ConfigureProperty(property, value);
            }
        }

        void IContainer.RegisterSingleton(Type lookupType, object instance)
        {
            ((IConfigurableApplicationContext)context).ObjectFactory.RegisterSingleton(lookupType.FullName, instance);
        }

        bool IContainer.HasComponent(Type componentType)
        {
            if (componentProperties.ContainsKey(componentType))
                return true;

            if (((IConfigurableApplicationContext) context).ObjectFactory.ContainsObjectDefinition(componentType.FullName))
                return true;

            if (((IConfigurableApplicationContext)context).ObjectFactory.ContainsSingleton(componentType.FullName))
                return true;

            foreach(var component in componentProperties.Keys)
                if (componentType.IsAssignableFrom(component))
                    return true;

            return false;
        }

        private void Init()
        {
            if (initialized)
                return;

            lock (componentProperties)
            {
                foreach (Type t in componentProperties.Keys)
                {
                    ObjectDefinitionBuilder builder = ObjectDefinitionBuilder.RootObjectDefinition(factory, t)
                        .SetAutowireMode(AutoWiringMode.AutoDetect)
                        .SetSingleton(typeHandleLookup[t] == ComponentCallModelEnum.Singleton);

                    componentProperties[t].Configure(builder);

                    IObjectDefinition def = builder.ObjectDefinition;
                    context.RegisterObjectDefinition(t.FullName, def);
                }
            }

            initialized = true;
        }

        private readonly Dictionary<Type, ComponentCallModelEnum> typeHandleLookup = new Dictionary<Type, ComponentCallModelEnum>();
        private readonly Dictionary<Type, ComponentConfig> componentProperties = new Dictionary<Type, ComponentConfig>();
        private bool initialized;
        private DefaultObjectDefinitionFactory factory = new DefaultObjectDefinitionFactory();
    }
}
