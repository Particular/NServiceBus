namespace NServiceBus
{
    using System;
    using NServiceBus.ObjectBuilder;
    using NServiceBus.ObjectBuilder.Common;

    class ComponentConfig : IComponentConfig
    {
        Type component;
        IContainer childContainer;

        public ComponentConfig(Type component, IContainer childContainer)
        {
            this.component = component;
            this.childContainer = childContainer;
        }

        IComponentConfig IComponentConfig.ConfigureProperty(string name, object value)
        {
            childContainer.ConfigureProperty(component, name, value);

            return this;
        }
    }

}
