using System;
using System.Collections.Generic;
using System.Linq;
using StructureMap;
using StructureMap.Attributes;
using StructureMap.Graph;
using StructureMap.Pipeline;
using IContainer = StructureMap.IContainer;

namespace NServiceBus.ObjectBuilder.StructureMap
{
    /// <summary>
    /// ObjectBuilder implementation for the StructureMap IoC-Container
    /// </summary>
    public class StructureMapObjectBuilder : Common.IContainer
    {
        private readonly IContainer container;
        private readonly IDictionary<Type, ConfiguredInstance> configuredInstances = new Dictionary<Type, ConfiguredInstance>();

        public StructureMapObjectBuilder()
        {
            container = ObjectFactory.GetInstance<IContainer>();
        }

        public StructureMapObjectBuilder(IContainer container)
        {
            this.container = container;
        }

        /// <summary>
        /// Gets the requested type, null is returned if type is not configured
        /// </summary>
        /// <param name="typeToBuild"></param>
        /// <returns></returns>
        object Common.IContainer.Build(Type typeToBuild)
        {
            return container.TryGetInstance(typeToBuild);

        }

        /// <summary>
        /// Returns all instances of the given type
        /// </summary>
        /// <param name="typeToBuild"></param>
        /// <returns></returns>
        IEnumerable<object> Common.IContainer.BuildAll(Type typeToBuild)
        {
            return container.GetAllInstances(typeToBuild).Cast<object>();
        }

        /// <summary>
        /// Configures a property value for the given type
        /// </summary>
        /// <param name="component"></param>
        /// <param name="property"></param>
        /// <param name="value"></param>
        void Common.IContainer.ConfigureProperty(Type component, string property, object value)
        {
            if (value == null)
            {
                return;
            }

            lock (configuredInstances)
            {
                ConfiguredInstance configuredInstance;
                configuredInstances.TryGetValue(component, out configuredInstance);

                if (configuredInstance == null)
                    throw new InvalidOperationException("Cannot configure property before the component has been configured. Please call 'Configure' first.");

                if (value.GetType().IsPrimitive || value is string)
                    configuredInstance.WithProperty(property).EqualTo(value);
                else
                    configuredInstance.Child(property).Is(value);
            }
        }

        /// <summary>
        /// Registers the given type in the conitainer
        /// </summary>
        /// <param name="component"></param>
        /// <param name="callModel"></param>
        void Common.IContainer.Configure(Type component, ComponentCallModelEnum callModel)
        {
            var scope = GetInstanceScopeFrom(callModel);

            ConfiguredInstance configuredInstance = null;

            container.Configure(x =>
            {
                configuredInstance = x.ForRequestedType(component)
                     .CacheBy(scope)
                     .TheDefaultIsConcreteType(component);

                foreach (var implementedInterface in GetAllInterfacesImplementedBy(component))
                {
                    x.RegisterAdditionalInterfaceForPluginType(implementedInterface, component);

                    x.EnableSetterInjectionFor(implementedInterface);
                }
            });

            lock (configuredInstances)
                configuredInstances.Add(component, configuredInstance);
        }

        /// <summary>
        /// Registers the given object as a singletion in the container
        /// </summary>
        /// <param name="lookupType"></param>
        /// <param name="instance"></param>
        void Common.IContainer.RegisterSingleton(Type lookupType, object instance)
        {

            container.Inject(lookupType, instance);
            PluginCache.AddFilledType(lookupType);
        }

        private static InstanceScope GetInstanceScopeFrom(ComponentCallModelEnum callModel)
        {
            switch (callModel)
            {
                case ComponentCallModelEnum.Singlecall: return InstanceScope.PerRequest;
                case ComponentCallModelEnum.Singleton: return InstanceScope.Singleton;
            }

            throw new ArgumentException("Unhandled call model:" + callModel);
        }

        private static IEnumerable<Type> GetAllInterfacesImplementedBy(Type t)
        {
            var result = new List<Type>();

            if (t == null)
                return result;

            var interfaces = t.GetInterfaces().Where(x => (
                !x.IsGenericType
                ));
            result.AddRange(interfaces);

            foreach (Type interfaceType in interfaces)
                result.AddRange(GetAllInterfacesImplementedBy(interfaceType));

            return result;
        }

    }
}
