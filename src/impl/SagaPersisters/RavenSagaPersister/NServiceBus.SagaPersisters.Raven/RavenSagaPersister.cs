using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NServiceBus.Persistence.Raven;
using NServiceBus.Saga;

namespace NServiceBus.SagaPersisters.Raven
{
    using global::Raven.Client;

    public class RavenSagaPersister : ISagaPersister
    {
        readonly RavenSessionFactory sessionFactory;
        readonly MethodInfo getSagaWithUniquePropertyMethod = 
            typeof(RavenSagaPersister).GetMethod("GetSagaWithUniqueProperty", BindingFlags.NonPublic | BindingFlags.Instance, null, CallingConventions.Any, new[] { typeof(KeyValuePair<string, object>) }, null);

        protected IDocumentSession Session { get { return sessionFactory.Session; } }

        public RavenSagaPersister(RavenSessionFactory sessionFactory)
        {
            this.sessionFactory = sessionFactory;
        }

        public void Save(ISagaEntity saga)
        {
            ValidateUniqueProperties(saga);
            Session.Store(saga);
        }

        public void Update(ISagaEntity saga)
        {
            //Do not re-save saga entity, since raven is tracking the entity
            ValidateUniqueProperties(saga);
        }

        public T Get<T>(Guid sagaId) where T : ISagaEntity
        {
            try
            {
                return Session.Load<T>(sagaId);
            }
            catch (InvalidCastException)
            {
                return default(T);
            }
        }

        public T Get<T>(string property, object value) where T : ISagaEntity
        {
            return Session.Advanced.LuceneQuery<T>()
                .WhereEquals(property, value)
                .WaitForNonStaleResults()
                .FirstOrDefault();
        }

        public void Complete(ISagaEntity saga)
        {
            Session.Advanced.DatabaseCommands.Delete(sessionFactory.Store.Conventions.FindTypeTagName(saga.GetType()) + "/" + saga.Id, null);
        }

        protected void ValidateUniqueProperties(ISagaEntity saga)
        {
            var uniqueProperties = UniqueAttribute.GetUniqueProperties(saga.GetType())
                .ToDictionary(p => p.Name, p => p.GetValue(saga, null));

            if (uniqueProperties.Count == 0) return;

            var uniqueProperty = uniqueProperties.First();

            var genericQuery = getSagaWithUniquePropertyMethod.MakeGenericMethod(saga.GetType());
            var sagaId = (Guid)genericQuery.Invoke(this, new object[] { uniqueProperty });

            if (sagaId != Guid.Empty && sagaId != saga.Id)
                throw new InvalidOperationException(string.Format("Cannot store a saga. The saga with id '{0}' already has property '{1}' with value '{2}'.", sagaId, uniqueProperty.Key, uniqueProperty.Value));
        }

        protected Guid GetSagaWithUniqueProperty<T>(KeyValuePair<string, object> uniqueProperty) where T : ISagaEntity
        {
            return Session.Advanced.LuceneQuery<T>()
                .WhereEquals(uniqueProperty.Key, uniqueProperty.Value)
                .WaitForNonStaleResults()
                .Select(s => s.Id)
                .FirstOrDefault();
        }
    }
}