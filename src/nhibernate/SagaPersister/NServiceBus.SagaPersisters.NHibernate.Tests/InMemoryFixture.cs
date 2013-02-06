namespace NServiceBus.SagaPersisters.NHibernate.Tests
{
    using System;
    using System.Collections.Specialized;
    using System.Configuration;
    using System.IO;
    using System.Security.Principal;
    using Config.Installer;
    using Config.Internal;
    using NUnit.Framework;
    using Persistence.NHibernate;
    using UnitOfWork;
    using UnitOfWork.NHibernate;
    using global::NHibernate;

    public class InMemoryFixture
    {
        protected IManageUnitsOfWork UnitOfWork;
        protected SagaPersister SagaPersister;
        protected ISessionFactory SessionFactory;

        private const string dialect = "NHibernate.Dialect.SQLiteDialect";

        [SetUp]
        public void SetUp()
        {
            string connectionString = String.Format(@"Data Source={0};Version=3;New=True;", Path.GetTempFileName());

            Configure.ConfigurationSource = new FakeConfigurationSource();

            NHibernateSettingRetriever.AppSettings = () => new NameValueCollection
                                                               {
                                                                   {"NServiceBus/Persistence/NHibernate/dialect", dialect}
                                                               };

            NHibernateSettingRetriever.ConnectionStrings = () => new ConnectionStringSettingsCollection
                                                                     {
                                                                         new ConnectionStringSettings("NServiceBus/Persistence/NHibernate/Saga", connectionString)
                                                                     };

            ConfigureNHibernate.Init();


            Configure.With(typeof(TestSaga).Assembly.GetTypes())
                .DefineEndpointName("Foo")
                .DefaultBuilder()
                .Sagas()
                .UseNHibernateSagaPersister();

            var builder = new SessionFactoryBuilder(Configure.TypesToScan);
            var properties = ConfigureNHibernate.SagaPersisterProperties;

            SessionFactory = builder.Build(ConfigureNHibernate.CreateConfigurationWith(properties));

            SagaPersister = new SagaPersister { SessionFactory = SessionFactory };

            UnitOfWork = new UnitOfWorkManager { SessionFactory = SessionFactory };

            new Installer().Install(WindowsIdentity.GetCurrent().Name);
        }

        [TearDown]
        public void Cleanup()
        {
            SessionFactory.Close();
        }
    }
}