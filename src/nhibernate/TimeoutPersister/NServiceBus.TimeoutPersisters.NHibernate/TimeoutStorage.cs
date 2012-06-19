namespace NServiceBus.TimeoutPersisters.NHibernate
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Data;
    using System.Linq;
    using Serializers.Json;
    using Timeout.Core;
    using global::NHibernate;

    public class TimeoutStorage : IPersistTimeouts
    {
        public ISessionFactory SessionFactory { get; set; }

        public IEnumerable<TimeoutData> GetAll()
        {
            using (var session = SessionFactory.OpenStatelessSession())
            using (var tx = session.BeginTransaction(IsolationLevel.ReadCommitted))
            {
                var timeoutEntities = session.QueryOver<TimeoutEntity>()
                    .Where(x => x.Endpoint == Configure.EndpointName)
                    .List();

                tx.Commit();

                return timeoutEntities.Select(te => new TimeoutData
                                                        {
                                                            CorrelationId = te.CorrelationId,
                                                            Destination = te.Destination,
                                                            Id = te.Id.ToString(),
                                                            SagaId = te.SagaId,
                                                            State = te.State,
                                                            Time = te.Time,
                                                            Headers = ConvertStringToDictionary(te.Headers),
                                                        });
            }
        }

        public void Add(TimeoutData timeout)
        {
            var newId = Guid.NewGuid();

            using (var session = SessionFactory.OpenSession())
            using (var tx = session.BeginTransaction(IsolationLevel.ReadCommitted))
            {
                session.Save(new TimeoutEntity
                {
                    Id = newId,
                    CorrelationId = timeout.CorrelationId,
                    Destination = timeout.Destination,
                    SagaId = timeout.SagaId,
                    State = timeout.State,
                    Time = timeout.Time,
                    Headers = ConvertDictionaryToString(timeout.Headers),
                    Endpoint = timeout.OwningTimeoutManager,
                });

                tx.Commit();
            }

            timeout.Id = newId.ToString();
        }

        public void Remove(string timeoutId)
        {
            using (var session = SessionFactory.OpenStatelessSession())
            using (var tx = session.BeginTransaction(IsolationLevel.ReadCommitted))
            {
                var queryString = string.Format("delete {0} where Id = :timeoutId",
                                        typeof(TimeoutEntity));
                session.CreateQuery(queryString)
                       .SetParameter("timeoutId", Guid.Parse(timeoutId))
                       .ExecuteUpdate();

                tx.Commit();
            }
        }

        public void ClearTimeoutsFor(Guid sagaId)
        {
            using (var session = SessionFactory.OpenStatelessSession())
            using (var tx = session.BeginTransaction(IsolationLevel.ReadCommitted))
            {
                var queryString = string.Format("delete {0} where SagaId = :sagaid",
                                        typeof(TimeoutEntity));
                session.CreateQuery(queryString)
                       .SetParameter("sagaid", sagaId)
                       .ExecuteUpdate();

                tx.Commit();
            }
        }

        static Dictionary<string,string> ConvertStringToDictionary(string data)
        {
            if (String.IsNullOrEmpty(data))
            {
                return new Dictionary<string, string>();
            }

            return serializer.DeserializeObject<Dictionary<string, string>>(data);
        }

        static string ConvertDictionaryToString(ICollection data)
        {
            if (data == null || data.Count == 0)
            {
                return null;
            }

            return serializer.SerializeObject(data);
        }

        static readonly JsonMessageSerializer serializer = new JsonMessageSerializer(null);
    }
}
