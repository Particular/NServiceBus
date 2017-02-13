namespace NServiceBus.Config
{
    using System.Configuration;

    /// <summary>
    /// A configuration element collection of <see cref="RijndaelExpiredKey" />s.
    /// </summary>
    [ObsoleteEx(
        Message = "Message property encryption is released as a dedicated 'NServiceBus.Encryption.MessageProperty' package.",
        RemoveInVersion = "8",
        TreatAsErrorFromVersion = "7")]
    public class RijndaelExpiredKeyCollection : ConfigurationElementCollection
    {
        /// <summary>
        /// Returns AddRemoveClearMap.
        /// </summary>
        public override ConfigurationElementCollectionType CollectionType => ConfigurationElementCollectionType.AddRemoveClearMap;

        /// <summary>
        /// Gets/sets the <see cref="RijndaelExpiredKey" /> at the given index.
        /// </summary>
        public RijndaelExpiredKey this[int index]
        {
            get { return (RijndaelExpiredKey) BaseGet(index); }
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
        /// Gets the <see cref="RijndaelExpiredKey" /> for the given key.
        /// </summary>
        new public RijndaelExpiredKey this[string key] => (RijndaelExpiredKey) BaseGet(key);

        /// <summary>
        /// Creates a new <see cref="RijndaelExpiredKey" />.
        /// </summary>
        protected override ConfigurationElement CreateNewElement()
        {
            return new RijndaelExpiredKey();
        }

        /// <summary>
        /// Creates a new <see cref="RijndaelExpiredKey" />, setting its <see cref="RijndaelExpiredKey.Key" /> property to the
        /// given value.
        /// </summary>
        protected override ConfigurationElement CreateNewElement(string elementName)
        {
            return new RijndaelExpiredKey
            {
                Key = elementName
            };
        }

        /// <summary>
        /// Returns the Messages property of the given <see cref="RijndaelExpiredKey" /> element.
        /// </summary>
        protected override object GetElementKey(ConfigurationElement element)
        {
            var encryptionKey = (RijndaelExpiredKey) element;

            return encryptionKey.Key;
        }

        /// <summary>
        /// Calls BaseIndexOf on the given <see cref="RijndaelExpiredKey" />.
        /// </summary>
        public int IndexOf(RijndaelExpiredKey encryptionKey)
        {
            return BaseIndexOf(encryptionKey);
        }

        /// <summary>
        /// Calls BaseAdd.
        /// </summary>
        public void Add(RijndaelExpiredKey mapping)
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
        /// If the key exists, calls BaseRemove on it.
        /// </summary>
        public void Remove(RijndaelExpiredKey mapping)
        {
            if (BaseIndexOf(mapping) >= 0)
            {
                BaseRemove(mapping.Key);
            }
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

        /// <summary>
        /// True if the collection is readonly.
        /// </summary>
        public override bool IsReadOnly()
        {
            return false;
        }
    }
}