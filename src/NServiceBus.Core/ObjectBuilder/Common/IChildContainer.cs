namespace NServiceBus.ObjectBuilder.Common
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Abstraction of a container.
    /// </summary>
    public interface IChildContainer : IDisposable
    {
        /// <summary>
        /// Returns an instantiation of the given type.
        /// </summary>
        /// <param name="typeToBuild">The <see cref="Type"/> to build.</param>
        /// <returns>The component instance.</returns>
        object Build(Type typeToBuild);

        /// <summary>
        /// Returns a list of objects instantiated because their type is compatible
        /// with the given type.
        /// </summary>
        /// <param name="typeToBuild">Type to be build.</param>
        /// <returns>Enumeration of all types that implement <paramref name="typeToBuild"/>.</returns>
        IEnumerable<object> BuildAll(Type typeToBuild);

        /// <summary>
        /// Indicates if a component of the given type has been configured.
        /// </summary>
        /// <param name="componentType">Component type to check.</param>
        /// <returns><c>true</c> if the <paramref name="componentType"/> is registered in the container or <c>false</c> otherwise.</returns>
        bool HasComponent(Type componentType);

        /// <summary>
        /// Releases a component instance.
        /// </summary>
        /// <param name="instance">The component instance to release.</param>
        void Release(object instance);
    }
}
