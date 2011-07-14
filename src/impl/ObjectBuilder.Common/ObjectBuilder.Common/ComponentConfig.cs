using System;
using System.Linq.Expressions;
using NServiceBus.Utils.Reflection;

namespace NServiceBus.ObjectBuilder.Common
{
    class ComponentConfig : IComponentConfig
    {
        private Type component;
        private IContainer container;

        public ComponentConfig(Type component, IContainer container)
        {
            this.component = component;
            this.container = container;
        }

        IComponentConfig IComponentConfig.ConfigureProperty(string name, object value)
        {
            this.container.ConfigureProperty(this.component, name, value);

            return this;
        }
    }

    class ComponentConfig<T> : ComponentConfig, IComponentConfig<T>
    {
        public ComponentConfig(IContainer container) : base(typeof(T), container)
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
