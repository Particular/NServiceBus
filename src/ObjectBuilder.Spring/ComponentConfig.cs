using System.Collections.Generic;
using Spring.Objects.Factory.Support;

namespace NServiceBus.ObjectBuilder.Spring
{
    class ComponentConfig : IComponentConfig
    {
        private readonly Dictionary<string, object> properties = new Dictionary<string, object>();

        public void Configure(ObjectDefinitionBuilder builder)
        {
            foreach (string key in this.properties.Keys)
                builder.AddPropertyValue(key, properties[key]);
        }

        #region IComponentConfig Members

        public IComponentConfig ConfigureProperty(string name, object value)
        {
            this.properties[name] = value;

            return this;
        }

        #endregion
    }
}
