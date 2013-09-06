namespace NServiceBus.Config
{
    using System.Collections.Generic;
    using System.Configuration;

    /// <summary>
    /// Collection of NHibernate properties
    /// </summary>
    public class NHibernatePropertyCollection: ConfigurationElementCollection
    {
        /// <summary>
        /// Creates a new empty property
        /// </summary>
        /// <returns></returns>
        protected override ConfigurationElement CreateNewElement()
        {
            return new NHibernateProperty();
        }

        /// <summary>
        /// Returns the key for the given element
        /// </summary>
        /// <param name="element"></param>
        /// <returns></returns>
        protected override object GetElementKey(ConfigurationElement element)
        {
            return ((NHibernateProperty)element).Key;
        }

        /// <summary>
        /// Converts the collection to a dictionary
        /// </summary>
        /// <returns></returns>
        public IDictionary<string,string> ToProperties()
        {
            var returnValue = new Dictionary<string, string>();

            foreach(var element in this)
            {

                returnValue.Add(
                    (element as NHibernateProperty).Key,
                    (element as NHibernateProperty).Value);
            }

            return returnValue;
        }
    }
}