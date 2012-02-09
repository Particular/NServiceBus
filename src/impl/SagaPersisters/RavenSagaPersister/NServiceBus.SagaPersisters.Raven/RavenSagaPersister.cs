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
            //Do not re-save saga entity, since raven is tracking the entity
            StoreUniqueProperty(saga);
        }

        public T Get<T>(Guid sagaId) where T : ISagaEntity
        {
            return Get<T>("Id", sagaId);
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
            DeleteUniqueProperty(saga);
        }
        
        void StoreUniqueProperty(ISagaEntity saga)
        {
            var uniqueProperty = UniqueAttribute.GetUniqueProperty(saga);

            if (!uniqueProperty.HasValue) return;

            var item = Session.Query<SagaUniqueIdentity>()
                .Customize(c => c.WaitForNonStaleResults(TimeSpan.FromSeconds(1)))
                .SingleOrDefault(i => i.SagaId == saga.Id);

            if(item == null)
            {
                var id = SagaUniqueIdentity.FormatId(saga.GetType(), uniqueProperty.Value);
                Session.Store(new SagaUniqueIdentity { Id = id, SagaId = saga.Id, UniqueValue = uniqueProperty.Value.Value});
                return;
            }

            if (item.SagaId != saga.Id)
                throw new InvalidOperationException(string.Format("Cannot store a saga. The saga with id '{0}' already has property '{1}' with value '{2}'.", saga.Id, uniqueProperty.Value.Key, uniqueProperty.Value.Value));

            if (item.SagaId == saga.Id && !item.UniqueValue.Equals(uniqueProperty.Value.Value))
                throw new InvalidOperationException("Cannot store a saga. The Raven saga persister does not support updating the value of unique properties");
        }

        void DeleteUniqueProperty(ISagaEntity saga)
        {
            var uniqueProperty = UniqueAttribute.GetUniqueProperty(saga);

            if (!uniqueProperty.HasValue) return;

            var id = SagaUniqueIdentity.FormatId(saga.GetType(), uniqueProperty.Value);

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