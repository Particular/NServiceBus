namespace NServiceBus.ObjectBuilder.Spring
{
    using System.Collections.Generic;
    using global::Spring.Objects.Factory.Support;

    class ComponentConfig : IComponentConfig
    {
        Dictionary<string, object> properties = new Dictionary<string, object>();

        public void Configure(ObjectDefinitionBuilder builder)
        {
            foreach (var key in properties.Keys)
            {
                builder.AddPropertyValue(key, properties[key]);
            }
        }

        public IComponentConfig ConfigureProperty(string name, object value)
        {
            properties[name] = value;

            return this;
        }
    }
}
