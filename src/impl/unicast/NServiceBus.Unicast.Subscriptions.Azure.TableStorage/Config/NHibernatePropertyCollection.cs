using System.Collections.Generic;
using System.Configuration;
using System.Linq;

namespace NServiceBus.Unicast.Subscriptions.Azure.TableStorage.Config
{
    public class NHibernatePropertyCollection : ConfigurationElementCollection
    {
        protected override ConfigurationElement CreateNewElement()
        {
            return new NHibernateProperty();
        }

        protected override object GetElementKey(ConfigurationElement element)
        {
            return ((NHibernateProperty)element).Key;
        }

        public IDictionary<string, string> ToProperties()
        {
            return this.Cast<object>().ToDictionary(element => ((NHibernateProperty) element).Key, element => ((NHibernateProperty) element).Value);
        }
    }
}