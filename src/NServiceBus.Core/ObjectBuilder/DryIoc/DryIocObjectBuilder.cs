namespace NServiceBus.ObjectBuilder
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using DryIoc;

    class DryIocObjectBuilder : Common.IContainer
    {
        IContainer container;

        public DryIocObjectBuilder()
        {
            container = new Container(rules => rules
                .WithoutThrowOnRegisteringDisposableTransient()
                .With(propertiesAndFields: PropertiesAndFields.Auto)
                .WithImplicitRootOpenScope());
        }

        DryIocObjectBuilder(IContainer openScope)
        {
            container = openScope;
        }

        public void Dispose()
        {
            // injected
        }

        public object Build(Type typeToBuild)
        {
            var result = container.Resolve(typeToBuild);
            return result;
        }

        public Common.IContainer BuildChildContainer()
        {
            return new DryIocObjectBuilder(container.OpenScope().WithNoMoreRegistrationAllowed());
        }

        public IEnumerable<object> BuildAll(Type typeToBuild)
        {
            return container.ResolveMany(typeToBuild);
        }

        public void Configure(Type component, DependencyLifecycle dependencyLifecycle)
        {
            if (container.IsRegistered(component))
            {
                return;
            }
            var lifetime = MapLifetime(dependencyLifecycle);
            container.RegisterMany(new[] { component }, lifetime
                , nonPublicServiceTypes: true
            );
        }

        IReuse MapLifetime(DependencyLifecycle dependencyLifecycle)
        {
            switch (dependencyLifecycle)
            {
                case DependencyLifecycle.SingleInstance:
                    return Reuse.Singleton;
                case DependencyLifecycle.InstancePerUnitOfWork:
                    return Reuse.InCurrentScope;
                case DependencyLifecycle.InstancePerCall:
                    return Reuse.Transient;
                default:
                    throw new Exception($"unmapped {nameof(DependencyLifecycle)} state: {dependencyLifecycle}");
            }
        }

        public void Configure<T>(Func<T> component, DependencyLifecycle dependencyLifecycle)
        {
            var lifetime = MapLifetime(dependencyLifecycle);
            container.RegisterDelegate(r => component(), lifetime);

            var interfaces = GetAllServices(typeof(T));
            foreach (var @interface in interfaces)
            {
                container.RegisterMapping(@interface, typeof(T));
            }
        }

        public void RegisterSingleton(Type lookupType, object instance)
        {
            container.RegisterInstance(lookupType, instance, Reuse.Singleton, ifAlreadyRegistered: IfAlreadyRegistered.Replace);
        }

        public bool HasComponent(Type componentType)
        {
            return container.IsRegistered(componentType);
        }

        public void Release(object instance)
        {
        }

        // take from the autofac objectbuilder
        static IEnumerable<Type> GetAllServices(Type type)
        {
            if (type == null)
            {
                return new List<Type>();
            }

            var result = new List<Type>(type.GetInterfaces());
            //{
            //    type
            //};

            foreach (var interfaceType in type.GetInterfaces())
            {
                result.AddRange(GetAllServices(interfaceType));
            }

            return result.Distinct();
        }
    }
}