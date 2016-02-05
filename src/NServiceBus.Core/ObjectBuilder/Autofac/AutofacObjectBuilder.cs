namespace NServiceBus
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using global::Autofac;
    using global::Autofac.Builder;
    using global::Autofac.Core;
    using NServiceBus.ObjectBuilder.Common;

    class AutofacObjectBuilder : global::NServiceBus.ObjectBuilder.Common.IContainer
    {
        ILifetimeScope container;

        public AutofacObjectBuilder(ILifetimeScope container)
        {
            this.container = container ?? new ContainerBuilder().Build();
        }

        public AutofacObjectBuilder()
            : this(null)
        {
        }

        public void Dispose()
        {
            //Injected at compile time
        }

        public IChildContainer BuildChildContainer()
        {
            return new AutofacObjectBuilder(container.BeginLifetimeScope());
        }

        public object Build(Type typeToBuild)
        {
            return container.Resolve(typeToBuild);
        }

        public IEnumerable<object> BuildAll(Type typeToBuild)
        {
            return ResolveAll(container, typeToBuild);
        }

        void global::NServiceBus.ObjectBuilder.Common.IContainer.Configure(Type component, DependencyLifecycle dependencyLifecycle)
        {
            var registration = GetComponentRegistration(component);

            if (registration != null)
            {
                return;
            }

            var builder = new ContainerBuilder();
            var services = GetAllServices(component).ToArray();
            var registrationBuilder = builder.RegisterType(component).As(services).PropertiesAutowired();

            SetLifetimeScope(dependencyLifecycle, registrationBuilder);

            builder.Update(container.ComponentRegistry);
        }

        void global::NServiceBus.ObjectBuilder.Common.IContainer.Configure<T>(Func<T> componentFactory, DependencyLifecycle dependencyLifecycle)
        {
            var registration = GetComponentRegistration(typeof(T));

            if (registration != null)
            {
                return;
            }

            var builder = new ContainerBuilder();
            var services = GetAllServices(typeof(T)).ToArray();
            var registrationBuilder = builder.Register(c => componentFactory.Invoke()).As(services).PropertiesAutowired();

            SetLifetimeScope(dependencyLifecycle, (IRegistrationBuilder<object, IConcreteActivatorData, SingleRegistrationStyle>) registrationBuilder);

            builder.Update(container.ComponentRegistry);
        }

        public void ConfigureProperty(Type component, string property, object value)
        {
            var registration = GetComponentRegistration(component);

            if (registration == null)
            {
                var message = "Cannot configure properties for a type that hasn't been configured yet: " + component.FullName;
                throw new InvalidOperationException(message);
            }

            registration.Activating += (sender, e) => SetPropertyValue(e.Instance, property, value);
        }

        public void RegisterSingleton(Type lookupType, object instance)
        {
            var builder = new ContainerBuilder();
            builder.RegisterInstance(instance).As(lookupType).PropertiesAutowired();
            builder.Update(container.ComponentRegistry);
        }

        public bool HasComponent(Type componentType)
        {
            return container.IsRegistered(componentType);
        }

        public void Release(object instance)
        {
        }

        static void SetPropertyValue(object instance, string propertyName, object value)
        {
            instance.GetType()
                .GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance)
                .SetValue(instance, value, null);
        }

        static void SetLifetimeScope(DependencyLifecycle dependencyLifecycle, IRegistrationBuilder<object, IConcreteActivatorData, SingleRegistrationStyle> registrationBuilder)
        {
            switch (dependencyLifecycle)
            {
                case DependencyLifecycle.InstancePerCall:
                    registrationBuilder.InstancePerDependency();
                    break;
                case DependencyLifecycle.SingleInstance:
                    registrationBuilder.SingleInstance();
                    break;
                case DependencyLifecycle.InstancePerUnitOfWork:
                    registrationBuilder.InstancePerLifetimeScope();
                    break;
                default:
                    throw new ArgumentException("Unhandled lifecycle - " + dependencyLifecycle);
            }
        }

        IComponentRegistration GetComponentRegistration(Type concreteComponent)
        {
            return container.ComponentRegistry.Registrations.FirstOrDefault(x => x.Activator.LimitType == concreteComponent);
        }

        static IEnumerable<Type> GetAllServices(Type type)
        {
            if (type == null)
            {
                return new List<Type>();
            }

            var result = new List<Type>(type.GetInterfaces())
                         {
                             type
                         };

            foreach (var interfaceType in type.GetInterfaces())
            {
                result.AddRange(GetAllServices(interfaceType));
            }

            return result.Distinct();
        }

        static IEnumerable<object> ResolveAll(IComponentContext container, Type componentType)
        {
            return container.Resolve(typeof(IEnumerable<>).MakeGenericType(componentType)) as IEnumerable<object>;
        }
    }
}