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

        public List<Tuple<string, DateTime>> GetNextChunk(DateTime startSlice, out DateTime nextTimeToRunQuery)
        {
            DateTime now = DateTime.UtcNow;
            
            using (var session = SessionFactory.OpenStatelessSession())
            using (var tx = session.BeginTransaction(IsolationLevel.ReadCommitted))
            {
                var results = session.QueryOver<TimeoutEntity>()
                    .Where(x => x.Endpoint == Configure.EndpointName)
                    .And(x => x.Time >= startSlice && x.Time <= now)
                    .OrderBy(x => x.Time).Asc
                    .Select(x => x.Id, x => x.Time)
                    .List <object[]>()
                    .Select(properties => new Tuple<string, DateTime>(((Guid)properties[0]).ToString(), (DateTime) properties[1]))
                    .ToList();

               //Retrieve next time we need to run query
                var startOfNextChunk = session.QueryOver<TimeoutEntity>()
                    .Where(x => x.Endpoint == Configure.EndpointName)
                    .Where(x => x.Time > now)
                    .OrderBy(x => x.Time).Asc
                    .Take(1)
                    .SingleOrDefault();

                if (startOfNextChunk != null)
                {
                    nextTimeToRunQuery = startOfNextChunk.Time;
                }
                else
                {
                    nextTimeToRunQuery = DateTime.UtcNow.AddMinutes(10);
                }

                tx.Commit();

                return results;
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

        public bool TryRemove(string timeoutId, out TimeoutData timeoutData)
        {
            using (var session = SessionFactory.OpenSession())
            using (var tx = session.BeginTransaction(IsolationLevel.ReadCommitted))
            {
                var te = session.Get<TimeoutEntity>(new Guid(timeoutId));

                if (te == null)
                {
					tx.Commit();
                    timeoutData = null;
                    return false;
                }

                timeoutData = new TimeoutData
                    {
                        CorrelationId = te.CorrelationId,
                        Destination = te.Destination,
                        Id = te.Id.ToString(),
                        SagaId = te.SagaId,
                        State = te.State,
                        Time = te.Time,
                        Headers = ConvertStringToDictionary(te.Headers),
                    };

                session.Delete(te);
                tx.Commit();

                return true;
            }
        }

        public void RemoveTimeoutBy(Guid sagaId)
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
