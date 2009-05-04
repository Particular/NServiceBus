using System;
using System.Collections.Generic;
using Spring.Context.Support;
using Spring.Objects.Factory.Support;
using Spring.Objects.Factory.Config;
using System.Collections;
using NServiceBus.ObjectBuilder.Common;
using NServiceBus.ObjectBuilder;

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

        #region IContainer Members

        object IContainer.Build(Type typeToBuild)
        {
            this.Init();
            IDictionary dict = context.GetObjectsOfType(typeToBuild, true, false);

            if (dict.Count == 0)
                return null;

            IDictionaryEnumerator de = dict.GetEnumerator();

            return (de.MoveNext() ? de.Value : null);
        }

        IEnumerable<object> IContainer.BuildAll(Type typeToBuild)
        {
            this.Init();
            IDictionary dict = context.GetObjectsOfType(typeToBuild, true, false);

            IDictionaryEnumerator de = dict.GetEnumerator();
            while (de.MoveNext())
                yield return de.Entry.Value;
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

        private readonly Dictionary<Type, ComponentCallModelEnum> typeHandleLookup = new Dictionary<Type, ComponentCallModelEnum>();
        private readonly Dictionary<Type, ComponentConfig> componentProperties = new Dictionary<Type, ComponentConfig>();
        private bool initialized;
        private DefaultObjectDefinitionFactory factory = new DefaultObjectDefinitionFactory();
    }
}
