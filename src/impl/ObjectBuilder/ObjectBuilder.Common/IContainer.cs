using System;
using System.Collections.Generic;

namespace NServiceBus.ObjectBuilder.Common
{
    /// <summary>
    /// Abstraction of a container.
    /// </summary>
    public interface IContainer
    {
        /// <summary>
        /// Returns an instantiation of the given type.
        /// </summary>
        /// <param name="typeToBuild"></param>
        /// <returns></returns>
        object Build(Type typeToBuild);

        /// <summary>
        /// Returns a list of objects instantiated because their type is compatible
        /// with the given type.
        /// </summary>
        /// <param name="typeToBuild"></param>
        /// <returns></returns>
        IEnumerable<object> BuildAll(Type typeToBuild);

        /// <summary>
        /// Configures the call model of the given component type.
        /// </summary>
        /// <param name="component"></param>
        /// <param name="callModel"></param>
        void Configure(Type component, ComponentCallModelEnum callModel);

        /// <summary>
        /// Sets the value to be configured for the given property of the 
        /// given component type.
        /// </summary>
        /// <param name="component"></param>
        /// <param name="property"></param>
        /// <param name="value"></param>
        void ConfigureProperty(Type component, string property, object value);

        /// <summary>
        /// Registers the given instance as the singleton that will be returned
        /// for the given type.
        /// </summary>
        /// <param name="lookupType"></param>
        /// <param name="instance"></param>
        void RegisterSingleton(Type lookupType, object instance);
    }
}
