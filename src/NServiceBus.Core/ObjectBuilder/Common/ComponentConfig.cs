namespace NServiceBus
{
    using System;
    using ObjectBuilder;
    using ObjectBuilder.Common;

    class ComponentConfig : IComponentConfig
    {
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

        Type component;
        IContainer container;
    }
}