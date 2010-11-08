using System.Linq.Expressions;
using System;
namespace NServiceBus.ObjectBuilder
{
    /// <summary>
    /// Used to configure the values to be set for the various
    /// properties on a component.
    /// </summary>
    public interface IComponentConfig
    {
        /// <summary>
        /// Configures the value of the named property of the component.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        IComponentConfig ConfigureProperty(string name, object value);
    }

    /// <summary>
    /// Strongly typed version of IComponentConfig
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public interface IComponentConfig<T>
    {
        /// <summary>
        /// Configures the value of the property like so:
        /// ConfigureProperty(o => o.Property, value);
        /// </summary>
        /// <param name="property"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        IComponentConfig<T> ConfigureProperty(Expression<Func<T, object>> property, object value);
    }
}
