namespace NServiceBus.SagaPersisters.Azure
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net;
    using System.Reflection;
    using Microsoft.WindowsAzure.Storage;
    using Microsoft.WindowsAzure.Storage.Table;
    using Saga;

    /// <summary>
    /// Saga persister implementation using NHibernate.
    /// </summary>
    public class AzureSagaPersister : ISagaPersister
    {
        readonly bool autoUpdateSchema;
        readonly CloudTableClient client;
        readonly ConcurrentDictionary<string, bool> tableCreated = new ConcurrentDictionary<string, bool>();

        /// <summary>
        /// 
        /// </summary>
        /// <param name="account"></param>
        /// <param name="autoUpdateSchema"></param>
        public AzureSagaPersister(CloudStorageAccount account, bool autoUpdateSchema)
        {
            this.autoUpdateSchema = autoUpdateSchema;
            client = account.CreateCloudTableClient();
        }

        /// <summary>
        /// Saves the given saga entity using the current session of the
        /// injected session factory.
        /// </summary>
        /// <param name="saga">the saga entity that will be saved.</param>
        public void Save(IContainSagaData saga)
        {
            Persist(saga);
        }

        /// <summary>
        /// Updates the given saga entity using the current session of the
        /// injected session factory.
        /// </summary>
        /// <param name="saga">the saga entity that will be updated.</param>
        public void Update(IContainSagaData saga)
        {
            Persist(saga);
        }

        /// <summary>
        /// Gets a saga entity from the injected session factory's current session
        /// using the given saga id.
        /// </summary>
        /// <param name="sagaId">The saga id to use in the lookup.</param>
        /// <returns>The saga entity if found, otherwise null.</returns>
        public T Get<T>(Guid sagaId) where T : IContainSagaData
        {
            var tableName = typeof(T).Name;
            var table = client.GetTableReference(tableName);

            var query = new TableQuery<DictionaryTableEntity>().Where(TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, sagaId.ToString()));

           return ToEntity<T>(table.ExecuteQuery(query).FirstOrDefault());
        }

       
        T ISagaPersister.Get<T>(string property, object value)
        {
            var type = typeof (T);
            var tableName = type.Name;
            var table = client.GetTableReference(tableName);

            TableQuery<DictionaryTableEntity> query;

            var propertyInfo = type.GetProperty(property);

            if (propertyInfo.PropertyType == typeof(byte[]))
            {
                query = new TableQuery<DictionaryTableEntity>().Where(TableQuery.GenerateFilterConditionForBinary(property, QueryComparisons.Equal, (byte[])value));
            }
            else if (propertyInfo.PropertyType == typeof(bool))
            {
                query = new TableQuery<DictionaryTableEntity>().Where(TableQuery.GenerateFilterConditionForBool(property, QueryComparisons.Equal, (bool) value));
            }
            else if (propertyInfo.PropertyType == typeof(DateTime))
            {
                query = new TableQuery<DictionaryTableEntity>().Where(TableQuery.GenerateFilterConditionForDate(property, QueryComparisons.Equal, (DateTime)value));
            }
            else if (propertyInfo.PropertyType == typeof(Guid))
            {
                query = new TableQuery<DictionaryTableEntity>().Where(TableQuery.GenerateFilterConditionForGuid(property, QueryComparisons.Equal, (Guid)value));
            }
            else if (propertyInfo.PropertyType == typeof(Int32))
            {
                query = new TableQuery<DictionaryTableEntity>().Where(TableQuery.GenerateFilterConditionForInt(property, QueryComparisons.Equal, (int)value));
            }
            else if (propertyInfo.PropertyType == typeof(Int64))
            {
                query = new TableQuery<DictionaryTableEntity>().Where(TableQuery.GenerateFilterConditionForLong(property, QueryComparisons.Equal, (long)value));
            }
            else if (propertyInfo.PropertyType == typeof(Double))
            {
                query = new TableQuery<DictionaryTableEntity>().Where(TableQuery.GenerateFilterConditionForDouble(property, QueryComparisons.Equal, (double)value));
            }
            else if (propertyInfo.PropertyType == typeof(string))
            {
                query = new TableQuery<DictionaryTableEntity>().Where(TableQuery.GenerateFilterCondition(property, QueryComparisons.Equal, (string)value));
            }
            else
            {
                throw new NotSupportedException(
                    string.Format("The property type '{0}' is not supported in windows azure table storage",
                                  propertyInfo.PropertyType.Name));
            }

            try
            {
                return ToEntity<T>(table.ExecuteQuery(query).FirstOrDefault());
            }
            catch (WebException ex)
            {
                // occurs when table has not yet been created, but already looking for absence of instance
                if (ex.Status == WebExceptionStatus.ProtocolError && ex.Response != null)
                {
                    var response = (HttpWebResponse) ex.Response;
                    if (response.StatusCode == HttpStatusCode.NotFound)
                    {
                        return default(T);
                    }
                }

                throw;
            }
            
        }

        /// <summary>
        /// Deletes the given saga from the injected session factory's
        /// current session.
        /// </summary>
        /// <param name="saga">The saga entity that will be deleted.</param>
        public void Complete(IContainSagaData saga)
        {
            try
            {
                var tableName = saga.GetType().Name;
                var table = client.GetTableReference(tableName);

                var query = new TableQuery<DictionaryTableEntity>().Where(TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, saga.Id.ToString()));

                var entity = table.ExecuteQuery(query).FirstOrDefault();

                table.Execute(TableOperation.Delete(entity));
            }
            catch (StorageException)
            {
                throw;
            }
           
        }

        void Persist(IContainSagaData saga)
        {
            var tableName = saga.GetType().Name;
            var table = client.GetTableReference(tableName);
            if (autoUpdateSchema && !tableCreated.ContainsKey(tableName))
            {
                table.CreateIfNotExists();
                tableCreated[tableName] = true;
            }

            var partitionKey = saga.Id.ToString();

            var batch = new TableBatchOperation();

            AddObjectToBatch(batch, saga, partitionKey);

            table.ExecuteBatch(batch);
        }

        static void AddObjectToBatch(TableBatchOperation batch, object entity, string partitionKey, string rowkey = "")
        {
            if (rowkey == "") rowkey = partitionKey; // just to be backward compat with original implementation

            var type = entity.GetType();

            var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);

            var toPersist = ToDictionaryTableEntity(entity, partitionKey, rowkey, properties);

            batch.Add(TableOperation.InsertOrReplace(toPersist));
        }

        static DictionaryTableEntity ToDictionaryTableEntity(object entity, string partitionKey, string rowkey, IEnumerable<PropertyInfo> properties)
        {
            var toPersist = new DictionaryTableEntity
                {
                    PartitionKey = partitionKey,
                    RowKey = rowkey
                };

            foreach (var propertyInfo in properties)
            {
                if (propertyInfo.PropertyType == typeof (byte[]))
                {
                    toPersist.Add(propertyInfo.Name, (byte[]) propertyInfo.GetValue(entity, null));
                }
                else if (propertyInfo.PropertyType == typeof (bool))
                {
                    toPersist.Add(propertyInfo.Name, (bool) propertyInfo.GetValue(entity, null));
                }
                else if (propertyInfo.PropertyType == typeof (DateTime))
                {
                    toPersist.Add(propertyInfo.Name, (DateTime) propertyInfo.GetValue(entity, null));
                }
                else if (propertyInfo.PropertyType == typeof (Guid))
                {
                    toPersist.Add(propertyInfo.Name, (Guid) propertyInfo.GetValue(entity, null));
                }
                else if (propertyInfo.PropertyType == typeof (Int32))
                {
                    toPersist.Add(propertyInfo.Name, (Int32) propertyInfo.GetValue(entity, null));
                }
                else if (propertyInfo.PropertyType == typeof (Int64))
                {
                    toPersist.Add(propertyInfo.Name, (Int64) propertyInfo.GetValue(entity, null));
                }
                else if (propertyInfo.PropertyType == typeof(Double))
                {
                    toPersist.Add(propertyInfo.Name, (Double)propertyInfo.GetValue(entity, null));
                }
                else if (propertyInfo.PropertyType == typeof (string))
                {
                    toPersist.Add(propertyInfo.Name, (string) propertyInfo.GetValue(entity, null));
                }
                else
                {
                    throw new NotSupportedException(
                        string.Format("The property type '{0}' is not supported in windows azure table storage",
                                      propertyInfo.PropertyType.Name));
                }
            }
            return toPersist;
        }

        T ToEntity<T>(DictionaryTableEntity entity)
        {
            if (entity == null) return default(T);

            var toCreate = Activator.CreateInstance<T>();
            var entityType = typeof (T);

            foreach(var propertyInfo in entityType.GetProperties())
            {
                if (entity.ContainsKey(propertyInfo.Name))
                {
                    if (propertyInfo.PropertyType == typeof(byte[]))
                    {
                        propertyInfo.SetValue(toCreate, entity[propertyInfo.Name].BinaryValue, null);
                    }
                    else if (propertyInfo.PropertyType == typeof(bool))
                    {
                        var boolean = entity[propertyInfo.Name].BooleanValue;
                        propertyInfo.SetValue(toCreate, boolean.HasValue && boolean.Value, null);
                    }
                    else if (propertyInfo.PropertyType == typeof(DateTime))
                    {
                        var dateTimeOffset = entity[propertyInfo.Name].DateTimeOffsetValue;
                        propertyInfo.SetValue(toCreate, dateTimeOffset.HasValue ? dateTimeOffset.Value.DateTime : default(DateTime), null);
                    }
                    else if (propertyInfo.PropertyType == typeof(Guid))
                    {
                        var guid = entity[propertyInfo.Name].GuidValue;
                        propertyInfo.SetValue(toCreate, guid.HasValue ? guid.Value : default(Guid), null);
                    }
                    else if (propertyInfo.PropertyType == typeof(Int32))
                    {
                        var int32 = entity[propertyInfo.Name].Int32Value;
                        propertyInfo.SetValue(toCreate, int32.HasValue ? int32.Value : default(Int32), null);
                    }
                    else if (propertyInfo.PropertyType == typeof(Double))
                    {
                        var d = entity[propertyInfo.Name].DoubleValue;
                        propertyInfo.SetValue(toCreate, d.HasValue ? d.Value : default(Int64), null);
                    }
                    else if (propertyInfo.PropertyType == typeof(Int64))
                    {
                        var int64 = entity[propertyInfo.Name].Int64Value;
                        propertyInfo.SetValue(toCreate, int64.HasValue ? int64.Value : default(Int64), null);
                    }
                    else if (propertyInfo.PropertyType == typeof(string))
                    {
                        propertyInfo.SetValue(toCreate, entity[propertyInfo.Name].StringValue, null);
                    }
                    else
                    {
                        throw new NotSupportedException(
                            string.Format("The property type '{0}' is not supported in windows azure table storage",
                                          propertyInfo.PropertyType.Name));
                    }
                    
                    
                }
            }

            return toCreate;
        }
    }
}
