namespace NServiceBus.ObjectBuilder.Spring
{
    using System;
    using System.Collections.Generic;
    using global::Spring.Context.Support;
    using Common;
    using global::Spring.Objects.Factory.Config;
    using global::Spring.Objects.Factory.Support;

    /// <summary>
    /// Implementation of <see cref="IContainer"/> using the Spring Framework container
    /// </summary>
    public class SpringObjectBuilder : IContainer
    {
        GenericApplicationContext context;
        bool isChildContainer;
        Dictionary<Type, DependencyLifecycle> typeHandleLookup = new Dictionary<Type, DependencyLifecycle>();
        Dictionary<Type, ComponentConfig> componentProperties = new Dictionary<Type, ComponentConfig>();
        bool initialized;
        DefaultObjectDefinitionFactory factory = new DefaultObjectDefinitionFactory();

        /// <summary>
        /// Instantiates the builder using a new <see cref="GenericApplicationContext"/>.
        /// </summary>
        public SpringObjectBuilder() : this(new GenericApplicationContext())
        {
        }

        /// <summary>
        /// Instantiates the builder using the given container.
        /// </summary>
        public SpringObjectBuilder(GenericApplicationContext context)
        {
            this.context = context;
        }

        public void Dispose()
        {
            //Injected at compile time
        }

        void DisposeManaged()
        {
            //This is to figure out if Dispose was called from a child container or not
            if (isChildContainer)
            {
                return;
            }

            if (context != null)
            {
                context.Dispose();
            }
        }

        public IContainer BuildChildContainer()
        {
            Init();
            return new SpringObjectBuilder(context)
                   {
                       isChildContainer = true,
                       initialized = true,
                       typeHandleLookup = typeHandleLookup,
                       componentProperties = componentProperties,
                       factory = factory
                   };
        }

        public object Build(Type typeToBuild)
        {
            Init();
            var dict = context.GetObjectsOfType(typeToBuild, true, false);

            var de = dict.GetEnumerator();

            if (de.MoveNext())
            {
                return de.Value;
            }
            
            throw new ArgumentException(string.Format("{0} has not been configured. In order to avoid this exception, check the return value of the 'HasComponent' method for this type.", typeToBuild));
        }

        public IEnumerable<object> BuildAll(Type typeToBuild)
        {
            Init();
            var dict = context.GetObjectsOfType(typeToBuild, true, false);

            var de = dict.GetEnumerator();
            while (de.MoveNext())
            {
                yield return de.Entry.Value;
            }
        }

        public void Configure(Type concreteComponent, DependencyLifecycle dependencyLifecycle)
        {
            if (initialized)
            {
                throw new InvalidOperationException("You can't alter the registrations after the container components has been resolved from the container");
            }

            typeHandleLookup[concreteComponent] = dependencyLifecycle;

            lock (componentProperties)
            {
                if (!componentProperties.ContainsKey(concreteComponent))
                {
                    componentProperties[concreteComponent] = new ComponentConfig();
                }
            }
        }

        public void Configure<T>(Func<T> componentFactory, DependencyLifecycle dependencyLifecycle)
        {
            var componentType = typeof(T);

            if (HasComponent(componentType))
            {
                return;
            }

            var funcFactory = new ArbitraryFuncDelegatingFactoryObject<T>(componentFactory, dependencyLifecycle == DependencyLifecycle.SingleInstance);
            
            context.ObjectFactory.RegisterSingleton(componentType.FullName, funcFactory);
        }

        public void ConfigureProperty(Type concreteComponent, string property, object value)
        {
            if (initialized)
            {
                throw new InvalidOperationException("You can't alter the registrations after the container components has been resolved from the container");
            }

            lock (componentProperties)
            {
                ComponentConfig result;
                componentProperties.TryGetValue(concreteComponent, out result);

                if (result == null)
                {
                    throw new InvalidOperationException("Cannot configure property before the component has been configured. Please call 'Configure' first.");
                }

                result.ConfigureProperty(property, value);
            }
        }

        public void RegisterSingleton(Type lookupType, object instance)
        {
            if (initialized)
            {
                throw new InvalidOperationException("You can't alter the registrations after the container components has been resolved from the container");
            }

            context.ObjectFactory.RegisterSingleton(lookupType.FullName, instance);
        }

        public bool HasComponent(Type componentType)
        {
            if (componentProperties.ContainsKey(componentType))
            {
                return true;
            }

            if (context.ObjectFactory.ContainsObjectDefinition(componentType.FullName))
            {
                return true;
            }

            if (context.ObjectFactory.ContainsSingleton(componentType.FullName))
            {
                return true;
            }

            foreach (var component in componentProperties.Keys)
            {
                if (componentType.IsAssignableFrom(component))
                {
                    return true;
                }
            }

            return false;
        }

        public void Release(object instance)
        {
        }

        void Init()
        {
            if (initialized)
            {
                return;
            }

            lock (componentProperties)
            {
                foreach (var t in componentProperties.Keys)
                {
                    var builder = ObjectDefinitionBuilder.RootObjectDefinition(factory, t)
                        .SetAutowireMode(AutoWiringMode.AutoDetect)
                        .SetSingleton(typeHandleLookup[t] == DependencyLifecycle.SingleInstance);                    

                    componentProperties[t].Configure(builder);

                    IObjectDefinition def = builder.ObjectDefinition;
                    context.RegisterObjectDefinition(t.FullName, def);
                }
            }

            initialized = true;
            context.Refresh();
        }

    }
}
