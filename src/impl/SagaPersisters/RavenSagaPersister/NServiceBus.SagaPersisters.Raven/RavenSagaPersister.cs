using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using NServiceBus.Persistence.Raven;
using NServiceBus.Saga;

namespace NServiceBus.SagaPersisters.Raven
{
    using global::Raven.Client;

    public class RavenSagaPersister : ISagaPersister
    {
        public const string UniqueValueMetadataKey = "NServiceBus-UniqueValue";
        
        readonly RavenSessionFactory sessionFactory;

        protected IDocumentSession Session { get { return sessionFactory.Session; } }

        public RavenSagaPersister(RavenSessionFactory sessionFactory)
        {
            this.sessionFactory = sessionFactory;
        }

        public void Save(ISagaEntity saga)
        {
            Session.Store(saga);
            StoreUniqueProperty(saga);
        }

        public void Update(ISagaEntity saga)
        {
            var p = UniqueAttribute.GetUniqueProperty(saga);

            if (!p.HasValue)
                return;
            
            var uniqueProperty = p.Value;

            var metadata = Session.Advanced.GetMetadataFor(saga);

            //if the user just added the unique property to a saga with existing data we need to set it
            if (!metadata.ContainsKey(UniqueValueMetadataKey))
            {
                StoreUniqueProperty(saga);
                return;
            }

            var storedvalue = metadata[UniqueValueMetadataKey].ToString();

            var currentValue = uniqueProperty.Value.ToString();

            if (currentValue == storedvalue)
                return;

            DeleteUniqueProperty(saga, new KeyValuePair<string, object>(uniqueProperty.Key,storedvalue));
            StoreUniqueProperty(saga);

        }

        public T Get<T>(Guid sagaId) where T : ISagaEntity
        {
            return Session.Load<T>(sagaId);
        }

        public T Get<T>(string property, object value) where T : ISagaEntity
        {
            try
            {
                return Session.Advanced.LuceneQuery<T>()
                    .WhereEquals(property, value)
                    .WaitForNonStaleResults()
                    .FirstOrDefault();
            }
            catch (InvalidCastException)
            {
                return default(T);
            }
        }

        public void Complete(ISagaEntity saga)
        {
            Session.Delete(saga);

            var uniqueProperty = UniqueAttribute.GetUniqueProperty(saga);

            if (!uniqueProperty.HasValue) 
                return;

            DeleteUniqueProperty(saga, uniqueProperty.Value);
        }

        void StoreUniqueProperty(ISagaEntity saga)
        {
            var uniqueProperty = UniqueAttribute.GetUniqueProperty(saga);

            if (!uniqueProperty.HasValue) return;

            var id = SagaUniqueIdentity.FormatId(saga.GetType(), uniqueProperty.Value);
            Session.Store(new SagaUniqueIdentity { Id = id, SagaId = saga.Id, UniqueValue = uniqueProperty.Value.Value });

            SetUniqueValueMetadata(saga, uniqueProperty.Value);
        }

        void SetUniqueValueMetadata(ISagaEntity saga, KeyValuePair<string, object> uniqueProperty)
        {
            Session.Advanced.GetMetadataFor(saga)[UniqueValueMetadataKey] = uniqueProperty.Value.ToString();
        }

        void DeleteUniqueProperty(ISagaEntity saga, KeyValuePair<string, object> uniqueProperty)
        {
            var id = SagaUniqueIdentity.FormatId(saga.GetType(), uniqueProperty);

            Session.Advanced.DatabaseCommands.Delete(id, null);
        }
    }

    public class SagaUniqueIdentity
    {
        public string Id { get; set; }
        public Guid SagaId { get; set; }
        public object UniqueValue { get; set; }

        public static string FormatId(Type sagaType, KeyValuePair<string, object> uniqueProperty)
        {
            //use MD5 hash to get a 16-byte hash of the string
            var provider = new MD5CryptoServiceProvider();
            var inputBytes = Encoding.Default.GetBytes(uniqueProperty.Value.ToString());
            var hashBytes = provider.ComputeHash(inputBytes);
            //generate a guid from the hash:
            var value = new Guid(hashBytes);

            return string.Format(string.Format("{0}/{1}/{2}", sagaType.FullName, uniqueProperty.Key, value));
        }
    }
}