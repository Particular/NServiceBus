using System;
using System.Collections.Generic;

namespace ObjectBuilder
{
    public interface IBuilder
    {
        /// <summary>
        /// Must be set to true in smart clients.
        /// </summary>
        bool JoinSynchronizationDomain { set; }

        /// <summary>
        /// Configures the given type. Can be used to configure all kinds of properties.
        /// </summary>
        /// <param name="concreteComponent"></param>
        /// <param name="callModel">Defines whether the type should have singleton or single call sematnics.</param>
        /// <returns></returns>
        IComponentConfig ConfigureComponent(Type concreteComponent, ComponentCallModelEnum callModel);
        
        /// <summary>
        /// Configures the given type. Can only be used to configure virtual properties.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="callModel">Defines whether the type should have singleton or single call sematnics.</param>
        /// <returns>An instance of type T.</returns>
        T ConfigureComponent<T>(ComponentCallModelEnum callModel);

        /// <summary>
        /// Creates an instance of the given type, injecting it with all defined dependencies.
        /// </summary>
        /// <param name="typeToBuild"></param>
        /// <returns></returns>
        object Build(Type typeToBuild);

        /// <summary>
        /// Creates an instance of the given type, injecting it with all defined dependencies.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        T Build<T>();

        /// <summary>
        /// For each type that is compatible with T, an instance is created with all dependencies injected, and yeilded to the caller.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        IEnumerable<T> BuildAll<T>();

        /// <summary>
        /// Builds an instance of the defined type injecting it with all defined dependencies
        /// and invokes the given method on the instance passing in the given parameters.
        /// </summary>
        /// <param name="typeToBuild"></param>
        /// <param name="methodName"></param>
        /// <param name="methodArgs"></param>
        void BuildAndDispatch(Type typeToBuild, string methodName, params object[] methodArgs);
    }
}
