//#define USESQL

using NHibernate.Cfg;
using NHibernate.Dialect;
using NHibernate.Mapping.ByCode;
using NHibernate.Tool.hbm2ddl;
using NUnit.Framework;
#if USESQL
using System.Data.SqlClient;
using NHibernate.Driver;
#else
using System.IO;
#endif

namespace NServiceBus.TimeoutPersisters.NHibernate.Tests
{
    using Timeout.Core;
    using global::NHibernate;

    public abstract class InMemoryDBFixture
    {
        protected IPersistTimeouts persister;
        protected ISessionFactory sessionFactory;

        [SetUp]
        public void SetupContext()
        {
#if USESQL
            var sqlBuilder = new SqlConnectionStringBuilder();
            sqlBuilder.DataSource = @".\SQLEXPRESS";
            sqlBuilder.InitialCatalog = "nservicebus";
            sqlBuilder.IntegratedSecurity = true;
            
            var cfg = new Configuration()

              .DataBaseIntegration(x =>
              {
                  x.Driver<Sql2008ClientDriver>();
                  x.Dialect<MsSql2008Dialect>();
                  x.ConnectionString = sqlBuilder.ConnectionString;
              });
#else
            var cfg = new Configuration()
                .DataBaseIntegration(x =>
                                         {
                                             x.Dialect<SQLiteDialect>();
                                             x.ConnectionString = string.Format(@"Data Source={0};Version=3;New=True;",
                                                                                Path.GetTempFileName());
                                         });
#endif

            var mapper = new ModelMapper();
            mapper.AddMappings(typeof (TimeoutEntity).Assembly.GetExportedTypes());
            var mapping = mapper.CompileMappingForAllExplicitlyAddedEntities();

            cfg.AddMapping(mapping);

            sessionFactory = cfg.BuildSessionFactory();
            persister = new TimeoutStorage
                            {
                                SessionFactory = sessionFactory
                            };

            Configure.Instance.DefineEndpointName("MyEndpoint");

            new SchemaUpdate(cfg).Execute(false, true);
        }
    }
}