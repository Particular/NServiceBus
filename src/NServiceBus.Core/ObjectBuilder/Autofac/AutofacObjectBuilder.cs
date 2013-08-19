namespace NServiceBus.ObjectBuilder.Autofac
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using global::Autofac;
    using global::Autofac.Builder;
    using global::Autofac.Core;

    ///<summary>
    /// Autofac implementation of <see cref="Common.IContainer"/>.
    ///</summary>
#if MAKE_AutofacObjectBuilder_INTERNAL
    internal class AutofacObjectBuilder : Common.IContainer
#else
    public class AutofacObjectBuilder : Common.IContainer
#endif
    {
        ILifetimeScope container;
        bool disposed;

        ///<summary>
        /// Instantiates the class utilizing the given container.
        ///</summary>
        public AutofacObjectBuilder(ILifetimeScope container)
        {
            this.container = container ?? new ContainerBuilder().Build();
        }

        ///<summary>
        /// Instantiates the class with an empty Autofac container.
        ///</summary>
        public AutofacObjectBuilder()
            : this(null)
        {
        }

        /// <summary>
        /// Disposes the container and all resources instantiated by the container.
        /// </summary>
        public void Dispose()
        {
            //Injected at compile time
        }

        /// <summary>
        /// Returns a child instance of the container to facilitate deterministic disposal
        /// of all resources built by the child container.
        /// </summary>
        public Common.IContainer BuildChildContainer()
        {
            return new AutofacObjectBuilder(container.BeginLifetimeScope());
        }

        ///<summary>
        /// Build an instance of a given type using Autofac.
        ///</summary>
        public object Build(Type typeToBuild)
        {
            return container.Resolve(typeToBuild);
        }

        ///<summary>
        /// Build all instances of a given type using Autofac.
        ///</summary>
        public IEnumerable<object> BuildAll(Type typeToBuild)
        {
            return ResolveAll(container, typeToBuild);
        }

        void Common.IContainer.Configure(Type component, DependencyLifecycle dependencyLifecycle)
        {
            var registration = GetComponentRegistration(component);

            if (registration != null)
                return;

            var builder = new ContainerBuilder();
            var services = GetAllServices(component).ToArray();
            var registrationBuilder = builder.RegisterType(component).As(services).PropertiesAutowired();            

            SetLifetimeScope(dependencyLifecycle, registrationBuilder);

            builder.Update(container.ComponentRegistry);
        }

        void Common.IContainer.Configure<T>(Func<T> componentFactory, DependencyLifecycle dependencyLifecycle)
        {
            var registration = GetComponentRegistration(typeof (T));

            if (registration != null)
                return;

            var builder = new ContainerBuilder();
            var services = GetAllServices(typeof(T)).ToArray();
            var registrationBuilder = builder.Register(c => componentFactory.Invoke()).As(services).PropertiesAutowired();            

            SetLifetimeScope(dependencyLifecycle, (IRegistrationBuilder<object, IConcreteActivatorData, SingleRegistrationStyle>) registrationBuilder);

            builder.Update(container.ComponentRegistry);
        }

        ///<summary>
        /// Configure the value of a named component property.
        ///</summary>
        public void ConfigureProperty(Type component, string property, object value)
        {
            var registration = GetComponentRegistration(component);

            if (registration == null)
            {
                throw new InvalidOperationException(
                    "Cannot configure properties for a type that hasn't been configured yet: " + component.FullName);
            }

            registration.Activating += (sender, e) => SetPropertyValue(e.Instance, property, value);
        }

        ///<summary>
        /// Register a singleton instance of a dependency within Autofac.
        ///</summary>
        ///<param name="lookupType"></param>
        ///<param name="instance"></param>
        public void RegisterSingleton(Type lookupType, object instance)
        {
            var builder = new ContainerBuilder();
            builder.RegisterInstance(instance).As(lookupType).PropertiesAutowired();
            builder.Update(this.container.ComponentRegistry);
        }

        public bool HasComponent(Type componentType)
        {
            return container.IsRegistered(componentType);
        }

        public void Release(object instance)
        {
        }

        ///<summary>
        /// Set a property value on an instance using reflection
        ///</summary>
        static void SetPropertyValue(object instance, string propertyName, object value)
        {
            instance.GetType().GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance).SetValue(instance, value, null);
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

            var result = new List<Type>(type.GetInterfaces()) {
                type
            };

            foreach (Type interfaceType in type.GetInterfaces())
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