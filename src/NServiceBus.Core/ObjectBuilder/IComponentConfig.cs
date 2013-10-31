namespace NServiceBus.ObjectBuilder
{
    using System;
    using System.Linq.Expressions;

    /// <summary>
    /// Used to configure the values to be set for the various
    /// properties on a component.
    /// </summary>
    public interface IComponentConfig
    {
        /// <summary>
        /// Configures the value of the named property of the component.
        /// </summary>
        IComponentConfig ConfigureProperty(string name, object value);
    }

    /// <summary>
    /// Strongly typed version of IComponentConfig
    /// </summary>
    public interface IComponentConfig<T>
    {
        /// <summary>
        /// Configures the value of the property like so:
        /// ConfigureProperty(o => o.Property, value);
        /// </summary>
        IComponentConfig<T> ConfigureProperty(Expression<Func<T, object>> property, object value);
    }
}
