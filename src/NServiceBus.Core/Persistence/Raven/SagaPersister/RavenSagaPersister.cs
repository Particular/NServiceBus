namespace NServiceBus.Persistence.Raven.SagaPersister
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Linq;
    using System.Security.Cryptography;
    using System.Text;
    using global::Raven.Abstractions.Commands;
    using global::Raven.Client;
    using global::Raven.Client.Document;
    using global::Raven.Json.Linq;
    using Saga;

    public class RavenSagaPersister : ISagaPersister
    {
        public const string UniqueValueMetadataKey = "NServiceBus-UniqueValue";

        readonly RavenSessionFactory sessionFactory;

        protected IDocumentSession Session { get { return sessionFactory.Session; } }

        public RavenSagaPersister(RavenSessionFactory sessionFactory)
        {
            this.sessionFactory = sessionFactory;
        }

        public void Save(IContainSagaData saga)
        {
            Session.Store(saga);
            StoreUniqueProperty(saga);
        }

        public void Update(IContainSagaData saga)
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

            var storedValue = metadata[UniqueValueMetadataKey].ToString();

            var currentValue = uniqueProperty.Value.ToString();

            if (currentValue == storedValue)
                return;

            DeleteUniqueProperty(saga, new KeyValuePair<string, object>(uniqueProperty.Key, storedValue));
            StoreUniqueProperty(saga);

        }

        public T Get<T>(Guid sagaId) where T : IContainSagaData
        {
            return Session.Load<T>(sagaId);
        }

        public T Get<T>(string property, object value) where T : IContainSagaData
        {
            if (IsUniqueProperty<T>(property))
                return GetByUniqueProperty<T>(property, value);

            return GetByQuery<T>(property, value).FirstOrDefault();
        }

        public void Complete(IContainSagaData saga)
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


        T GetByUniqueProperty<T>(string property, object value) where T : IContainSagaData
        {
            var lookupId = SagaUniqueIdentity.FormatId(typeof(T), new KeyValuePair<string, object>(property, value));

            var lookup = Session
                .Include("SagaDocId") //tell raven to pull the saga doc as well to save us a round-trip
                .Load<SagaUniqueIdentity>(lookupId);

            if (lookup != null)
                return lookup.SagaDocId != null
                    ? Session.Load<T>(lookup.SagaDocId) //if we have a saga id we can just load it
                    : Get<T>(lookup.SagaId); //if not this is a saga that was created pre 3.0.4 so we fallback to a get instead

            return default(T);
        }

        IEnumerable<T> GetByQuery<T>(string property, object value) where T : IContainSagaData
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

        void StoreUniqueProperty(IContainSagaData saga)
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

        void SetUniqueValueMetadata(IContainSagaData saga, KeyValuePair<string, object> uniqueProperty)
        {
            var s = (dynamic)Session.Advanced;

            var metadata = s.GetMetadataFor(saga);

            metadata[UniqueValueMetadataKey] = uniqueProperty.Value.ToString();
        }

        void DeleteUniqueProperty(IContainSagaData saga, KeyValuePair<string, object> uniqueProperty)
        {
            var id = SagaUniqueIdentity.FormatId(saga.GetType(), uniqueProperty);

            var s = (dynamic) Session.Advanced;
            var command = (dynamic)new DeleteCommandData
            {
                Key = id
            };

            //this doesn't work
            s.Defer (command);
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
            if (uniqueProperty.Value == null)
                throw new ArgumentNullException("uniqueProperty", string.Format("Property {0} is marked with the [Unique] attribute on {1} but contains a null value. Please make sure that all unique properties are set on your SagaData and/or that you have marked the correct properties as unique.", uniqueProperty.Key, sagaType.Name));

            // use MD5 hash to get a 16-byte hash of the string
            using (var provider = new MD5CryptoServiceProvider())
            {
                var inputBytes = Encoding.Default.GetBytes(uniqueProperty.Value.ToString());
                var hashBytes = provider.ComputeHash(inputBytes);

                // generate a guid from the hash:
                var value = new Guid(hashBytes);

                var id = string.Format("{0}/{1}/{2}", sagaType.FullName.Replace('+', '-'), uniqueProperty.Key, value);

                // raven has a size limit of 255 bytes == 127 unicode chars
                if (id.Length > 127)
                {
                    // generate a guid from the hash:
                    var key =
                        new Guid(
                            provider.ComputeHash(Encoding.Default.GetBytes(sagaType.FullName + uniqueProperty.Key)));

                    id = string.Format("MoreThan127/{0}/{1}", key, value);
                }

                return id;
            }
        }
    }
}