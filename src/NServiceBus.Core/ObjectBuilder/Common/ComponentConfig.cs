namespace NServiceBus
{
    using System;
    using NServiceBus.ObjectBuilder;
    using NServiceBus.ObjectBuilder.Common;

    class ComponentConfig : IComponentConfig
    {
        Type component;
        IContainer container;

        public ComponentConfig(Type component, IContainer container)
        {
            this.component = component;
            this.container = container;
        }

        IComponentConfig IComponentConfig.ConfigureProperty(string name, object value)
        {
            container.ConfigureProperty(component, name, value);

            return this;
        }
    }

}
