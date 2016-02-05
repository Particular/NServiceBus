namespace NServiceBus
{
    using System;
    using System.Linq.Expressions;
    using NServiceBus.ObjectBuilder;
    using NServiceBus.ObjectBuilder.Common;

    class ComponentConfig<T> : ComponentConfig, IComponentConfig<T>
    {
        public ComponentConfig(IContainer childContainer) : base(typeof(T), childContainer)
        {
        }         

        IComponentConfig<T> IComponentConfig<T>.ConfigureProperty(Expression<Func<T, object>> property, object value)
        {
            var prop = Reflect<T>.GetProperty(property);

            ((IComponentConfig)this).ConfigureProperty(prop.Name, value);

            return this;
        }
    }

}
