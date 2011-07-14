using System;
using System.Configuration;
using System.Collections;

namespace NServiceBus.Config
{
    /// <summary>
    /// A configuration element collection of MessageEndpointMappings.
    /// </summary>
    public class MessageEndpointMappingCollection : ConfigurationElementCollection
    {
        /// <summary>
        /// Returns AddRemoveClearMap.
        /// </summary>
        public override ConfigurationElementCollectionType CollectionType
        {
            get
            {
                return 
                    ConfigurationElementCollectionType.AddRemoveClearMap;
            }
        }

        /// <summary>
        /// Creates a new MessageEndpointMapping.
        /// </summary>
        /// <returns></returns>
        protected override ConfigurationElement CreateNewElement()
        {
            return new MessageEndpointMapping();
        }

        /// <summary>
        /// Creates a new MessageEndpointMapping, setting its Message property to the given name.
        /// </summary>
        /// <param name="elementName"></param>
        /// <returns></returns>
        protected override ConfigurationElement CreateNewElement(string elementName)
        {
            MessageEndpointMapping result = new MessageEndpointMapping();
            result.Messages = elementName;

            return result;
        }

        /// <summary>
        /// Returns the Messages property of the given MessageEndpointMapping element.
        /// </summary>
        /// <param name="element"></param>
        /// <returns></returns>
        protected override Object GetElementKey(ConfigurationElement element)
        {
            return ((MessageEndpointMapping)element).Messages;
        }

        /// <summary>
        /// Calls the base AddElementName.
        /// </summary>
        public new string AddElementName
        {
            get
            { return base.AddElementName; }

            set
            { base.AddElementName = value; }

        }

        /// <summary>
        /// Calls the base ClearElementName.
        /// </summary>
        public new string ClearElementName
        {
            get
            { return base.ClearElementName; }

            set
            { base.AddElementName = value; }

        }

        /// <summary>
        /// Returns the base RemoveElementName.
        /// </summary>
        public new string RemoveElementName
        {
            get
            { return base.RemoveElementName; }
        }

        /// <summary>
        /// Returns the base Count.
        /// </summary>
        public new int Count
        {
            get { return base.Count; }
        }

        /// <summary>
        /// Gets/sets the MessageEndpointMapping at the given index.
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        public MessageEndpointMapping this[int index]
        {
            get
            {
                return (MessageEndpointMapping)BaseGet(index);
            }
            set
            {
                if (BaseGet(index) != null)
                {
                    BaseRemoveAt(index);
                }
                BaseAdd(index, value);
            }
        }

        /// <summary>
        /// Gets the MessageEndpointMapping for the given name.
        /// </summary>
        /// <param name="Name"></param>
        /// <returns></returns>
        new public MessageEndpointMapping this[string Name]
        {
            get
            {
                return (MessageEndpointMapping)BaseGet(Name);
            }
        }

        /// <summary>
        /// Calls BaseIndexOf on the given mapping.
        /// </summary>
        /// <param name="mapping"></param>
        /// <returns></returns>
        public int IndexOf(MessageEndpointMapping mapping)
        {
            return BaseIndexOf(mapping);
        }

        /// <summary>
        /// Calls BaseAdd.
        /// </summary>
        /// <param name="mapping"></param>
        public void Add(MessageEndpointMapping mapping)
        {
            BaseAdd(mapping);
        }

        /// <summary>
        /// Calls BaseAdd with true as the additional parameter.
        /// </summary>
        /// <param name="element"></param>
        protected override void BaseAdd(ConfigurationElement element)
        {
            BaseAdd(element, true);
        }

        /// <summary>
        /// If the mapping exists, calls BaseRemove on it.
        /// </summary>
        /// <param name="mapping"></param>
        public void Remove(MessageEndpointMapping mapping)
        {
            if (BaseIndexOf(mapping) >= 0)
                BaseRemove(mapping.Messages);
        }

        /// <summary>
        /// Calls BaseRemoveAt.
        /// </summary>
        /// <param name="index"></param>
        public void RemoveAt(int index)
        {
            BaseRemoveAt(index);
        }

        /// <summary>
        /// Calls BaseRemove.
        /// </summary>
        /// <param name="name"></param>
        public void Remove(string name)
        {
            BaseRemove(name);
        }

        /// <summary>
        /// Calls BaseClear.
        /// </summary>
        public void Clear()
        {
            BaseClear();
        }
    }
}
