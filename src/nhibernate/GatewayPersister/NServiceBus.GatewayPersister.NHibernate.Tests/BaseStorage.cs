namespace NServiceBus.GatewayPersister.NHibernate.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Specialized;
    using System.Configuration;
    using System.Data;
    using System.IO;
    using System.Linq;
    using System.Security.Principal;
    using NHibernate;
    using Config;
    using NUnit.Framework;
    using Persistence.NHibernate;
    using Serializers.Json;

    public abstract class BaseStorage
    {
        protected GatewayPersister Persister;
        
        private readonly string connectionString = String.Format(@"Data Source={0};Version=3;New=True;", Path.GetTempFileName());
        private const string dialect = "NHibernate.Dialect.SQLiteDialect";

        [SetUp]
        public void Setup()
        {
            NHibernateSettingRetriever.AppSettings = () => new NameValueCollection
                                                               {
                                                                   {"NServiceBus/Persistence/NHibernate/dialect", dialect}
                                                               };

            NHibernateSettingRetriever.ConnectionStrings = () => new ConnectionStringSettingsCollection
                                                                     {
                                                                         new ConnectionStringSettings("NServiceBus/Persistence/NHibernate/Gateway", connectionString)
                                                                     };

            Configure.With(Enumerable.Empty<Type>())
                .DefineEndpointName("Foo")
                .DefaultBuilder()
                .UseNHibernateGatewayPersister();

            Persister = Configure.Instance.Builder.Build<GatewayPersister>();

            new Installer.Installer().Install(WindowsIdentity.GetCurrent().Name);
        }

        static Dictionary<string, string> ConvertStringToDictionary(string data)
        {
            if (String.IsNullOrEmpty(data))
            {
                return new Dictionary<string, string>();
            }

            return Serializer.DeserializeObject<Dictionary<string, string>>(data);
        }

        static readonly JsonMessageSerializer Serializer = new JsonMessageSerializer(null);

        protected bool Store(TestMessage message)
        {
            using (var msgStream = new MemoryStream(message.OriginalMessage))
            {
                var result = Persister.InsertMessage(message.ClientId, message.TimeReceived, msgStream, message.Headers);
               
                return result;
            }
        }

        protected TestMessage GetStoredMessage(string clientId)
        {
            using (var session = Persister.SessionFactory.OpenSession())
            using (var tx = session.BeginTransaction(IsolationLevel.ReadCommitted)) 
            {
                var messageStored = session.Get<GatewayMessage>(clientId);

                var msg = new TestMessage
                {
                    ClientId = messageStored.Id,
                    Headers = ConvertStringToDictionary(messageStored.Headers),
                    TimeReceived = messageStored.TimeReceived,
                    OriginalMessage = messageStored.OriginalMessage,
                    Acknowledged = messageStored.Acknowledged
                };

                tx.Commit();

                return msg;
            }
        }

        protected TestMessage CreateTestMessage()
        {
            var headers = new Dictionary<string, string>
                              {
                                  {"Header1", "Value1"},
                                  {"Header2", "Value2"},
                                  {"Header3", "49710.06:28:15"}
                              };

            return new TestMessage
            {
                ClientId = Guid.NewGuid().ToString(),
                TimeReceived = DateTime.UtcNow,
                Headers = headers,
                OriginalMessage = new byte[] { 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8 }
            };
        }
    }
}