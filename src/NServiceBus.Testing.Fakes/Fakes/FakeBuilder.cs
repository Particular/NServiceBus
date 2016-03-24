namespace NServiceBus.Testing
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using NServiceBus.ObjectBuilder;

    /// <summary>
    /// A fake implementation of <see cref="IBuilder"/> for testing purposes.
    /// </summary>
    public class FakeBuilder : IBuilder
    {
        IDictionary<Type, object[]> instances = new Dictionary<Type, object[]>();
        IDictionary<Type, Func<object[]>> factories = new Dictionary<Type, Func<object[]>>();

        /// <summary>
        /// Registers an instance which will be returned every time an instance of <typeparamref name="T"/> is resolved.
        /// </summary>
        public void Register<T>(params T[] instance) where T: class
        {
            instances.Add(typeof(T), instance);
        }

        /// <summary>
        /// Registers a factory method which will be invoked every time an instance of <typeparamref name="T"/> is resolved.
        /// </summary>
        public void Register<T>(Func<T> factory)
        {
            factories.Add(typeof(T), () => new object[]
            {
                factory()
            });
        }

        /// <summary>
        /// Registers a factory method which will be invoked every time an instance of <typeparamref name="T"/> is resolved.
        /// </summary>
        public void Register<T>(Func<T[]> factory) where T: class
        {
            factories.Add(typeof(T), factory);
        }

        /// <summary>
        /// Returns an instantiation of the given type.
        /// </summary>
        /// <param name="typeToBuild">The <see cref="T:System.Type"/> to build.</param>
        /// <returns>
        /// The component instance.
        /// </returns>
        public virtual object Build(Type typeToBuild)
        {
            return BuildAll(typeToBuild).First();
        }

        /// <summary>
        /// Creates an instance of the given type, injecting it with all defined dependencies.
        /// </summary>
        /// <typeparam name="T">Type to be resolved.</typeparam>
        /// <returns>
        /// Instance of <typeparamref name="T"/>.
        /// </returns>
        public virtual T Build<T>()
        {
            return BuildAll<T>().First();
        }

        /// <summary>
        /// For each type that is compatible with the given type, an instance is created with all dependencies injected.
        /// </summary>
        /// <param name="typeToBuild">The <see cref="T:System.Type"/> to build.</param>
        /// <returns>
        /// The component instances.
        /// </returns>
        public virtual IEnumerable<object> BuildAll(Type typeToBuild)
        {
            if (instances.ContainsKey(typeToBuild))
            {
                return instances[typeToBuild];
            }

            if (factories.ContainsKey(typeToBuild))
            {
                return factories[typeToBuild]();
            }

            throw new Exception($"Cannot build instance of type {typeToBuild} because there was no instance of factory registered for it.");
        }

        /// <summary>
        /// For each type that is compatible with T, an instance is created with all dependencies injected, and yielded to the
        /// caller.
        /// </summary>
        /// <typeparam name="T">Type to be resolved.</typeparam>
        /// <returns>
        /// Instances of <typeparamref name="T"/>.
        /// </returns>
        public virtual IEnumerable<T> BuildAll<T>()
        {
            var type = typeof(T);

            if (instances.ContainsKey(type))
            {
                return instances[type].Cast<T>();
            }

            if (factories.ContainsKey(type))
            {
                return factories[type]().Cast<T>();
            }

            throw new Exception($"Cannot build instance of type {type} because there was no instance of factory registered for it.");
        }

        /// <summary>
        /// Builds an instance of the defined type injecting it with all defined dependencies
        /// and invokes the given action on the instance.
        /// </summary>
        /// <param name="typeToBuild">The <see cref="T:System.Type"/> to build.</param><param name="action">The callback to call.</param>
        public virtual void BuildAndDispatch(Type typeToBuild, Action<object> action)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Returns a child instance of the container to facilitate deterministic disposal
        /// of all resources built by the child container.
        /// </summary>
        /// <returns>
        /// Returns a new child container.
        /// </returns>
        public virtual IBuilder CreateChildBuilder()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        /// <filterpriority>2</filterpriority>
        public virtual void Dispose()
        {
        }

        /// <summary>
        /// Releases a component instance.
        /// </summary>
        /// <param name="instance">The component instance to release.</param>
        public virtual void Release(object instance)
        {
        }
    }
}