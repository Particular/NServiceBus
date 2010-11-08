using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Spring.Objects.Factory.Support;
using NServiceBus.ObjectBuilder;

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
