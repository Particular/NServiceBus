using System.Collections.Generic;
using System.Configuration;

namespace NServiceBus.Unicast.Subscriptions.NHibernate.Config
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
            var retval = new Dictionary<string, string>();

            foreach (var element in this)
            {

                retval.Add(
                    (element as NHibernateProperty).Key,
                    (element as NHibernateProperty).Value);
            }

            return retval;
        }
    }
}