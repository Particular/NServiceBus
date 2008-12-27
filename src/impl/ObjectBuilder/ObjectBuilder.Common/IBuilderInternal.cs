using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NServiceBus.ObjectBuilder;

namespace NServiceBus.ObjectBuilder.Common
{
    /// <summary>
    /// Interface that will need to be implemented for various containers.
    /// </summary>
    public interface IBuilderInternal
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
        /// Instantiates the given type and then invokes the given action upon the instance.
        /// </summary>
        /// <param name="typeToBuild"></param>
        /// <param name="action"></param>
        void BuildAndDispatch(Type typeToBuild, Action<object> action);

        /// <summary>
        /// Registers the given type in the container with the given call model,
        /// returning an IComponentConfig.
        /// </summary>
        /// <param name="concreteComponent"></param>
        /// <param name="callModel"></param>
        /// <returns></returns>
        IComponentConfig ConfigureComponent(Type concreteComponent, ComponentCallModelEnum callModel);

        /// <summary>
        /// Registers the given type in the container with the given call model,
        /// creates a proxy for the object used for configuring properties as an 
        /// alternative to IComponentConfig.
        /// 
        /// This allows for the following code:
        /// <example>
        /// Configurer.ConfigureComponent{MyClass}(ComponentCallModelEnum.Singlecall)
        ///        .PropertyOnMyClass = someValue;
        /// </example>
        /// </summary>
        /// <param name="concreteComponent"></param>
        /// <param name="callModel"></param>
        /// <returns></returns>
        object Configure(Type concreteComponent, ComponentCallModelEnum callModel);
    }
}
