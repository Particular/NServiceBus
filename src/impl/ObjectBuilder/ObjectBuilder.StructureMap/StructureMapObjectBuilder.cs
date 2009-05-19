using System;
using System.Collections.Generic;
using System.Linq;
using Common.Logging;
using StructureMap;
using StructureMap.Attributes;
using StructureMap.Graph;
using StructureMap.Interceptors;
using StructureMap.Pipeline;
using IContainer = StructureMap.IContainer;

namespace NServiceBus.ObjectBuilder.StructureMap
{
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


        object Common.IContainer.Build(Type typeToBuild)
        {
            return container.GetInstance(typeToBuild);
        }

        IEnumerable<object> Common.IContainer.BuildAll(Type typeToBuild)
        {
            return container.GetAllInstances(typeToBuild).Cast<object>();
        }


        void Common.IContainer.ConfigureProperty(Type component, string property, object value)
        {
            if (value == null)
            {
                return;
            }


            LogManager.GetLogger("ObjectBuilder").Debug("Configuring property for " + component.Name +
                                                                ", propertyName: " + property +
                                                                " withvalue: " + value);
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

        void Common.IContainer.Configure(Type component, ComponentCallModelEnum callModel)
        {
            var scope = GetInstanceScopeFrom(callModel);

            ConfiguredInstance configuredInstance = null;

            container.Configure(x =>
            {
                configuredInstance = x.ForRequestedType(component)
                     .CacheBy(scope)
                     .TheDefaultIsConcreteType(component);

                foreach (var serviceType in GetAllInterfacesImplementedBy(component))
                {
                    x.ForRequestedType(serviceType)
                        .TheDefaultIsConcreteType(component)
                        .Interceptor = new RedirectToConcreteTypeInterceptor(component, container);

                    PluginCache.AddFilledType(serviceType);
                }


            });

            lock (configuredInstances)
                configuredInstances.Add(component, configuredInstance);
        }

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

    public class RedirectToConcreteTypeInterceptor : InstanceInterceptor
    {
        private readonly Type baseType;
        private readonly IContainer container;




        public RedirectToConcreteTypeInterceptor(Type baseType, IContainer container)
        {
            this.baseType = baseType;
            this.container = container;
        }


        public object Process(object target, IContext context)
        {
            return container.GetInstance(baseType);
        }

    }

}
