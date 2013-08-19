using System.Collections.Generic;
using Spring.Objects.Factory.Support;

namespace NServiceBus.ObjectBuilder.Spring
{
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
