﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using NServiceBus.Persistence.Raven;
using NServiceBus.Saga;

namespace NServiceBus.SagaPersisters.Raven
{
    using System.Collections.Concurrent;
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
            if (IsUniqueProperty<T>(property))
                return GetByUniqueProperty<T>(property, value);

            return GetByQuery<T>(property, value).FirstOrDefault();
        }

       
        public void Complete(ISagaEntity saga)
        {
            Session.Delete(saga);

            var uniqueProperty = UniqueAttribute.GetUniqueProperty(saga);

            if (!uniqueProperty.HasValue) 
                return;

            DeleteUniqueProperty(saga, uniqueProperty.Value);
        }
        bool IsUniqueProperty<T>(string property)
        {
            var key = typeof(T).FullName + property;
            bool value;

            if (!PropertyCache.TryGetValue(key, out value))
            {
                value = UniqueAttribute.GetUniqueProperties(typeof(T)).Any(p => p.Name == property);
                PropertyCache[key] = value;
            }

            return value;
        }


        T GetByUniqueProperty<T>(string property, object value) where T : ISagaEntity
        {
            var lookupId = SagaUniqueIdentity.FormatId(typeof(T), new KeyValuePair<string, object>(property, value));

            var lookup = Session
                //The line below is disable because of a bug in Raven 616. We can enable it when we upgrade to the lastest RavenDB
                //.Include("SagaDocId") //tell raven to pull the saga doc as well to save us a roundtrip
                .Load<SagaUniqueIdentity>(lookupId);

            if (lookup != null)
                return lookup.SagaDocId != null
                    ? Session.Load<T>(lookup.SagaDocId) //if we have a saga id we can just load it
                    : Get<T>(lookup.SagaId); //if not this is a saga that was created pre 3.0.4 so we fallback to a get instead


            return default(T);
        }

        IEnumerable<T> GetByQuery<T>(string property, object value) where T : ISagaEntity
        {
            try
            {
                return Session.Advanced.LuceneQuery<T>()
                    .WhereEquals(property, value)
                    .WaitForNonStaleResultsAsOfNow();
            }
            catch (InvalidCastException)
            {
                return new[] { default(T) };
            }
        }

        void StoreUniqueProperty(ISagaEntity saga)
        {
            var uniqueProperty = UniqueAttribute.GetUniqueProperty(saga);

            if (!uniqueProperty.HasValue) return;

            var id = SagaUniqueIdentity.FormatId(saga.GetType(), uniqueProperty.Value);
            var sagaDocId = sessionFactory.Store.Conventions.FindFullDocumentKeyFromNonStringIdentifier(saga.Id, saga.GetType(), false);
        
            Session.Store(new SagaUniqueIdentity
                              {
                                  Id = id, 
                                  SagaId = saga.Id, 
                                  UniqueValue = uniqueProperty.Value.Value,
                                  SagaDocId = sagaDocId
                              });

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

        static readonly ConcurrentDictionary<string, bool> PropertyCache = new ConcurrentDictionary<string, bool>();
    }

    public class SagaUniqueIdentity
    {
        public string Id { get; set; }
        public Guid SagaId { get; set; }
        public object UniqueValue { get; set; }
        public string SagaDocId { get; set; }

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