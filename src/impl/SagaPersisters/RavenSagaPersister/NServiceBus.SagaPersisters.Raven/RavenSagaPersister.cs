﻿using System;
using System.Linq;
using NServiceBus.Saga;
using Raven.Client;

namespace NServiceBus.SagaPersisters.Raven
{
    public class RavenSagaPersister : ISagaPersister
    {
        public IDocumentStore Store { get; set; }

        public string Endpoint { get; set; }

        public void Save(ISagaEntity saga)
        {
            using (var session = Store.OpenSession())
            {
                session.Store(saga);
                session.SaveChanges();
            }
        }

        public void Update(ISagaEntity saga)
        {
            using (var session = Store.OpenSession()) {
                session.SaveChanges();
            }
        }

        public T Get<T>(Guid sagaId) where T : ISagaEntity
        {
            using (var session = Store.OpenSession())
            {
                return session.Load<T>(sagaId);
            }
        }

        public T Get<T>(string property, object value) where T : ISagaEntity
        {
            var luceneQuery = string.Format("{0}:{1}", property, value);

            using (var session = Store.OpenSession())
            {
                return session.Advanced.LuceneQuery<T>()
                .Where(luceneQuery).FirstOrDefault();
            }
        }

        public void Complete(ISagaEntity saga)
        {
            using (var session = Store.OpenSession())
            {
                session.Delete(saga);
            }
        }
    }
}