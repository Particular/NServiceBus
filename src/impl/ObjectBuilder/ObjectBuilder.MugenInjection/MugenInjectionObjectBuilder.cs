using System;
using System.Collections.Generic;
using System.Linq;
using MugenInjection;
using MugenInjection.Interface;
using MugenInjection.Parameters;
using MugenInjection.Scope;
using NServiceBus.ObjectBuilder.Common;

namespace NServiceBus.ObjectBuilder.MugenInjection
{
    /// <summary>
    /// Mugen injection implementaton of <see cref="IContainer"/>.
    /// </summary>
    public class MugenInjectionObjectBuilder : IContainer
    {
        #region Fields

        private readonly IInjector _injector;

        #endregion

        #region Constructor

        /// <summary>
        /// Instantites the class with a new <see cref="MugenInjector"/>.
        /// </summary>
        public MugenInjectionObjectBuilder()
            : this(new MugenInjector())
        {
        }

        /// <summary>
        /// Instantiates the class saving the given container.
        /// </summary>
        /// <param name="injector"></param>
        public MugenInjectionObjectBuilder(IInjector injector)
        {
            if (injector == null) throw new ArgumentNullException("injector");
            _injector = injector;
        }

        #endregion

        #region Implementation of IDisposable

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        /// <filterpriority>2</filterpriority>
        public void Dispose()
        {
            _injector.Dispose();
        }

        #endregion

        #region Implementation of IContainer

        /// <summary>
        /// Returns an instantiation of the given type.
        /// </summary>
        /// <param name="typeToBuild"/>
        /// <returns/>
        public object Build(Type typeToBuild)
        {
            if (!HasComponent(typeToBuild))
                throw new ArgumentException(string.Format("The type {0} is not registered in the container.", typeToBuild));
            return _injector.Get(typeToBuild);
        }

        /// <summary>
        /// Returns a child instance of the container to facilitate deterministic disposal
        /// of all resources built by the child container.
        /// </summary>
        /// <returns></returns>
        public IContainer BuildChildContainer()
        {
            return new MugenInjectionObjectBuilder(_injector.CreateChild());
        }

        /// <summary>
        /// Returns a list of objects instantiated because their type is compatible
        ///             with the given type.
        /// </summary>
        /// <param name="typeToBuild"/>
        /// <returns/>
        public IEnumerable<object> BuildAll(Type typeToBuild)
        {
            return _injector.GetAll(typeToBuild);
        }

        /// <summary>
        /// Configures the call model of the given component type.
        /// </summary>
        /// <param name="component">Type to be configured</param><param name="dependencyLifecycle">The desired lifecycle for this type</param>
        public void Configure(Type component, DependencyLifecycle dependencyLifecycle)
        {
            if (HasComponent(component))
                return;

            var types = GetAllServiceTypesFor(component);
            var builderActivator = new ObjectBuilderActivator();
            ScopeLifecycleBase lifecycle = GetScopeLifecycle(dependencyLifecycle);
            _injector.BindWithManualBuild(types)
                .To(component)
                .InScope(lifecycle)
                .UseCustomActivator(builderActivator)
                .TryDisposeObjects()
                .Build();
        }

        public void Configure<T>(Func<T> component, DependencyLifecycle dependencyLifecycle)
        {
            Type componentType = typeof(T);
            if (HasComponent(componentType))
                return;

            var types = GetAllServiceTypesFor(componentType);
            var builderActivator = new ObjectBuilderActivator();
            ScopeLifecycleBase scopeLifecycleBase = GetScopeLifecycle(dependencyLifecycle);
            _injector.BindWithManualBuild(types)
                .ToMethod(context => component())
                .InScope(scopeLifecycleBase)
                .UseCustomActivator(builderActivator)
                .TryDisposeObjects()
                .Build();
        }

        /// <summary>
        /// Sets the value to be configured for the given property of the 
        ///             given component type.
        /// </summary>
        /// <param name="component"/><param name="property"/><param name="value"/>
        public void ConfigureProperty(Type component, string property, object value)
        {
            IEnumerable<IBinding> bindings = _injector.GetBindings(component);
            if (!bindings.Any())
                throw new ArgumentException(string.Format("Component {0} not registered.", component), "component");

            foreach (IBinding binding in bindings)
                binding.Parameters.Add(new PropertyParameter(property, value));
        }

        /// <summary>
        /// Registers the given instance as the singleton that will be returned
        ///             for the given type.
        /// </summary>
        /// <param name="lookupType"/><param name="instance"/>
        public void RegisterSingleton(Type lookupType, object instance)
        {
            if (HasComponent(lookupType))
                _injector.Unbind(lookupType);

            _injector.BindWithManualBuild(lookupType)
                .ToConstant(instance)
                .TryDisposeObjects()
                .Build();
        }

        /// <summary>
        /// Indicates if a component of the given type has been configured.
        /// </summary>
        /// <param name="componentType"/>
        /// <returns/>
        public bool HasComponent(Type componentType)
        {
            return _injector.CanResolve(componentType, true, false);
        }

        #endregion

        #region Method

        /// <summary>
        /// Gets all service types of a given component.
        /// </summary>
        /// <param name="component">The component.</param>
        /// <returns>All service types.</returns>
        private static Type[] GetAllServiceTypesFor(Type component)
        {
            if (component == null)
                return new Type[0];

            var result = new List<Type>(component.GetInterfaces()) { component };
            foreach (Type interfaceType in component.GetInterfaces())
                result.AddRange(GetAllServiceTypesFor(interfaceType));

            return result.Distinct().ToArray();
        }

        /// <summary>
        /// Get lifecycle scope.
        /// </summary>
        /// <param name="dependencyLifecycle"></param>
        /// <returns></returns>
        private static ScopeLifecycleBase GetScopeLifecycle(DependencyLifecycle dependencyLifecycle)
        {
            ScopeLifecycleBase scopeLifecycle;
            switch (dependencyLifecycle)
            {
                case DependencyLifecycle.SingleInstance:
                    scopeLifecycle = new SingletonScopeLifecycle();
                    break;
                case DependencyLifecycle.InstancePerUnitOfWork:
                    scopeLifecycle = new UnitOfWorkScopeLifecycle();
                    break;
                case DependencyLifecycle.InstancePerCall:
                    scopeLifecycle = new TransientScopeLifecycle();
                    break;
                default:
                    throw new ArgumentOutOfRangeException("dependencyLifecycle");
            }
            return scopeLifecycle;
        }

        #endregion
    }
}