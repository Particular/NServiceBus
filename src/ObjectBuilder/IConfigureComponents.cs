using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ObjectBuilder
{
    public interface IConfigureComponents
    {
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
    }
}
