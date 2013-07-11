namespace NServiceBus.SagaPersisters.NHibernate.Tests
{
    using System;
    using System.Collections.Specialized;
    using System.Configuration;
    using System.IO;
    using System.Linq;
    using System.Security.Principal;
    using Config.Installer;
    using Config.Internal;
    using NUnit.Framework;
    using Persistence.NHibernate;
    using Saga;
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

            Configure.Features.Enable<Features.Sagas>();

            var types = typeof(TestSaga).Assembly.GetTypes().ToList();
            types.Add(typeof(ContainSagaData));

            Configure.With(types)
                .DefineEndpointName("Foo")
                .DefaultBuilder()
                .UseNHibernateSagaPersister();

            var builder = new SessionFactoryBuilder(Configure.TypesToScan);
            var properties = ConfigureNHibernate.SagaPersisterProperties;

            SessionFactory = builder.Build(ConfigureNHibernate.CreateConfigurationWith(properties));

            UnitOfWork = new UnitOfWorkManager { SessionFactory = SessionFactory };

            SagaPersister = new SagaPersister { UnitOfWorkManager = (UnitOfWorkManager)UnitOfWork };

            new Installer().Install(WindowsIdentity.GetCurrent().Name);
        }

        [TearDown]
        public void Cleanup()
        {
            SessionFactory.Close();
        }
    }
}