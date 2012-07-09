namespace NServiceBus.Distributor.NHibernate.Tests
{
    using System;
    using System.Collections.Specialized;
    using System.Configuration;
    using System.IO;
    using System.Linq;
    using System.Security.Principal;
    using NUnit.Framework;
    using Persistence.NHibernate;

    public abstract class InMemoryDBFixture
    {
        protected NHibernateWorkerAvailabilityManager persister;

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
                                                                         new ConnectionStringSettings("NServiceBus/Persistence/NHibernate/Distributor", connectionString)
                                                                     };

            Configure.With(Enumerable.Empty<Type>())
                .DefineEndpointName("Foo")
                .DefaultBuilder()
                .UseNHibernateDistributor();

            persister = Configure.Instance.Builder.Build<NHibernateWorkerAvailabilityManager>();

            new Installer.Installer().Install(WindowsIdentity.GetCurrent());
        }
    }
}