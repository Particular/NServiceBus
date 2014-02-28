namespace NServiceBus.Config
{
    using System;
    using System.Configuration;

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
        protected override ConfigurationElement CreateNewElement()
        {
            return new MessageEndpointMapping();
        }

        /// <summary>
        /// Creates a new MessageEndpointMapping, setting its Message property to the given name.
        /// </summary>
        protected override ConfigurationElement CreateNewElement(string elementName)
        {
            var result = new MessageEndpointMapping {Messages = elementName};

            return result;
        }

        /// <summary>
        /// Returns the Messages property of the given MessageEndpointMapping element.
        /// </summary>
        protected override Object GetElementKey(ConfigurationElement element)
        {
            var messageEndpointMapping = (MessageEndpointMapping) element;

            return String.Format("{0}{1}{2}{3}", messageEndpointMapping.Messages, messageEndpointMapping.AssemblyName, messageEndpointMapping.TypeFullName, messageEndpointMapping.Namespace);
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
        public int IndexOf(MessageEndpointMapping mapping)
        {
            return BaseIndexOf(mapping);
        }

        /// <summary>
        /// Calls BaseAdd.
        /// </summary>
        public void Add(MessageEndpointMapping mapping)
        {
            BaseAdd(mapping);
        }

        /// <summary>
        /// Calls BaseAdd with true as the additional parameter.
        /// </summary>
        protected override void BaseAdd(ConfigurationElement element)
        {
            BaseAdd(element, true);
        }

        /// <summary>
        /// If the mapping exists, calls BaseRemove on it.
        /// </summary>
        public void Remove(MessageEndpointMapping mapping)
        {
            if (BaseIndexOf(mapping) >= 0)
                BaseRemove(mapping.Messages);
        }

        /// <summary>
        /// Calls BaseRemoveAt.
        /// </summary>
        public void RemoveAt(int index)
        {
            BaseRemoveAt(index);
        }

        /// <summary>
        /// Calls BaseRemove.
        /// </summary>
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

        public override bool IsReadOnly()
        {
            return false;
        }
    }
}
