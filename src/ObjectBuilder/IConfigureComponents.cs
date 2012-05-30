using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Linq.Expressions;

namespace NServiceBus.ObjectBuilder
{
    /// <summary>
    /// Used to configure components in the container.
    /// Should primarily be used at startup/initialization time.
    /// </summary>
    public interface IConfigureComponents
    {
        /// <summary>
        /// Configures the given type. Can be used to configure all kinds of properties.
        /// </summary>
        /// <param name="concreteComponent"></param>
        /// <param name="dependencyLifecycle">Defines lifecyle sematnics for the given type.</param>
        /// <returns></returns>
        IComponentConfig ConfigureComponent(Type concreteComponent, DependencyLifecycle dependencyLifecycle);

        /// <summary>
        /// Configures the given type, allowing to fluently configure properties.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="dependencyLifecycle">Defines lifecyle sematnics for the given type.</param>
        /// <returns></returns>
        IComponentConfig<T> ConfigureComponent<T>(DependencyLifecycle dependencyLifecycle);

        /// <summary>
        /// Configures the given type, allowing to fluently configure properties.
        /// </summary>
        /// <typeparam name="T">Type to configure</typeparam>
        /// <param name="componentFactory">Factory method that returns the given type</param>
        /// <param name="dependencyLifecycle">Defines lifecyle sematnics for the given type.</param>
        /// <returns></returns>
        IComponentConfig<T> ConfigureComponent<T>(Func<T> componentFactory, DependencyLifecycle dependencyLifecycle);

        /// <summary>
        /// Configures the given type. Can be used to configure all kinds of properties. This method is deprecated use the signature
        /// that contains the DependecyLifecyle enum instead
        /// </summary>
        /// <param name="concreteComponent"></param>
        /// <param name="callModel">Defines whether the type should have singleton or single call sematnics.</param>
        /// <returns></returns>
        [Obsolete]
        IComponentConfig ConfigureComponent(Type concreteComponent, ComponentCallModelEnum callModel);

        /// <summary>
        /// Configures the given type, allowing to fluently configure properties. This method is deprecated use the signature
        /// that contains the DependecyLifecyle enum instead
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="callModel"></param>
        /// <returns></returns>
        [Obsolete]
        IComponentConfig<T> ConfigureComponent<T>(ComponentCallModelEnum callModel);

        /// <summary>
        /// Configures the given property of the given type to be injected with the given value.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="property"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        IConfigureComponents ConfigureProperty<T>(Expression<Func<T, object>> property, object value);

        /// <summary>
        /// Registers the given instance as the singleton that will be returned
        /// for the given type.
        /// </summary>
        /// <param name="lookupType"></param>
        /// <param name="instance"></param>
        /// <returns></returns>
        IConfigureComponents RegisterSingleton(Type lookupType, object instance);

        /// <summary>
        /// Registers the given instance as the singleton that will be returned
        /// for the given type.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="instance"></param>
        /// <returns></returns>
        IConfigureComponents RegisterSingleton<T>(object instance);

        /// <summary>
        /// Indicates if a component of the given type has been configured.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        bool HasComponent<T>();

        /// <summary>
        /// Indicates if a component of the given type has been configured.
        /// </summary>
        /// <param name="componentType"></param>
        /// <returns></returns>
        bool HasComponent(Type componentType);
    }
}
