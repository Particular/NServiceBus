namespace NServiceBus.SagaPersisters.Azure
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using Microsoft.WindowsAzure.Storage;
    using Microsoft.WindowsAzure.Storage.Table;

    /// <summary>
    /// 
    /// </summary>
    public class DictionaryTableEntity : TableEntity, IDictionary<string, EntityProperty>
    {
        private IDictionary<string, EntityProperty> properties;

        /// <summary>
        /// 
        /// </summary>
        public DictionaryTableEntity()
        {
            properties = new Dictionary<string, EntityProperty>();
        }

        public override void ReadEntity(IDictionary<string, EntityProperty> entityProperties, OperationContext operationContext)
        {
            properties = entityProperties;
        }

        public override IDictionary<string, EntityProperty> WriteEntity(OperationContext operationContext)
        {
            return properties;
        }

        public void Add(string key, EntityProperty value)
        {
            properties.Add(key, value);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        public void Add(string key, bool value)
        {
            properties.Add(key, new EntityProperty(value));
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        public void Add(string key, byte[] value)
        {
            properties.Add(key, new EntityProperty(value));
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        public void Add(string key, DateTime? value)
        {
            properties.Add(key, new EntityProperty(value));
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        public void Add(string key, DateTimeOffset? value)
        {
            properties.Add(key, new EntityProperty(value));
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        public void Add(string key, double value)
        {
            properties.Add(key, new EntityProperty(value));
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        public void Add(string key, Guid value)
        {
            properties.Add(key, new EntityProperty(value));
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        public void Add(string key, int value)
        {
            properties.Add(key, new EntityProperty(value));
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        public void Add(string key, long value)
        {
            properties.Add(key, new EntityProperty(value));
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        public void Add(string key, string value)
        {
            properties.Add(key, new EntityProperty(value));
        }

        public bool ContainsKey(string key)
        {
            return properties.ContainsKey(key);
        }

        public ICollection<string> Keys
        {
            get { return properties.Keys; }
        }

        public bool Remove(string key)
        {
            return properties.Remove(key);
        }

        public bool TryGetValue(string key, out EntityProperty value)
        {
            return properties.TryGetValue(key, out value);
        }

        public ICollection<EntityProperty> Values
        {
            get { return properties.Values; }
        }

        public EntityProperty this[string key]
        {
            get { return properties[key]; }
            set { properties[key] = value; }
        }

        public void Add(KeyValuePair<string, EntityProperty> item)
        {
            properties.Add(item);
        }

        public void Clear()
        {
            properties.Clear();
        }

        public bool Contains(KeyValuePair<string, EntityProperty> item)
        {
            return properties.Contains(item);
        }

        public void CopyTo(KeyValuePair<string, EntityProperty>[] array, int arrayIndex)
        {
            properties.CopyTo(array, arrayIndex);
        }

        public int Count
        {
            get { return properties.Count; }
        }

        public bool IsReadOnly
        {
            get { return properties.IsReadOnly; }
        }

        public bool Remove(KeyValuePair<string, EntityProperty> item)
        {
            return properties.Remove(item);
        }

        public IEnumerator<KeyValuePair<string, EntityProperty>> GetEnumerator()
        {
            return properties.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return properties.GetEnumerator();
        }
    }
}