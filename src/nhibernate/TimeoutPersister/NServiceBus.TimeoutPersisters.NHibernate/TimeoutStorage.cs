namespace NServiceBus.TimeoutPersisters.NHibernate
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Data;
    using System.IO;
    using System.Linq;
    using System.Text;
    using MessageInterfaces;
    using Serializers.Json;
    using Timeout.Core;
    using global::NHibernate;

    public class TimeoutStorage : IPersistTimeouts
    {
        private readonly ISessionFactory sessionFactory;

        public TimeoutStorage(ISessionFactory sessionFactory)
        {
            this.sessionFactory = sessionFactory;
        }

        public IEnumerable<TimeoutData> GetAll()
        {
            using (var session = sessionFactory.OpenStatelessSession())
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

            using (var session = sessionFactory.OpenSession())
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
                    Endpoint = Configure.EndpointName,
                });

                tx.Commit();
            }

            timeout.Id = newId.ToString();
        }

        public void Remove(string timeoutId)
        {
            using (var session = sessionFactory.OpenStatelessSession())
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
            using (var session = sessionFactory.OpenStatelessSession())
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

            object[] objects;
            using (var stream = new MemoryStream(Encoding.UTF8.GetBytes(data)))
            {
                objects = serializer.Deserialize(stream);
            }

            if (objects.Length == 0)
            {
                return new Dictionary<string, string>();
            }

            return objects[0] as Dictionary<string, string>;
        }

        static string ConvertDictionaryToString(ICollection data)
        {
            if (data == null || data.Count == 0)
            {
                return null;
            }

            using (var stream = new MemoryStream())
            using (var writer = new StreamWriter(stream, Encoding.UTF8))
            {
                serializer.Serialize(new[] {(object) data}, writer.BaseStream);
                writer.Flush();

                stream.Position = 0;

                using (var reader = new StreamReader(stream, Encoding.UTF8))
                {
                    return reader.ReadToEnd();
                }
            }
        }

        static readonly JsonMessageSerializer serializer = new JsonMessageSerializer(new DictionaryMessageMapper());

        class DictionaryMessageMapper : IMessageMapper
        {
            public T CreateInstance<T>()
            {
                return default(T);
            }

            public T CreateInstance<T>(Action<T> action)
            {
                return default(T);
            }

            public object CreateInstance(Type messageType)
            {
                return null;
            }

            public void Initialize(IEnumerable<Type> types)
            {

            }

            public Type GetMappedTypeFor(Type t)
            {
                return null;
            }

            public Type GetMappedTypeFor(string typeName)
            {
                return null;
            }
        }
    }
}
