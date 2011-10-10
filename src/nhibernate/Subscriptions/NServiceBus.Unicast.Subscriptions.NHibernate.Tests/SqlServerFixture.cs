using System.Data.SqlClient;
using System.IO;
using NHibernate.Cfg;
using NHibernate.Cfg.MappingSchema;
using NHibernate.Dialect;
using NHibernate.Mapping.ByCode;
using NHibernate.Tool.hbm2ddl;
using NUnit.Framework;

namespace NServiceBus.Unicast.Subscriptions.NHibernate.Tests
{
  [TestFixture]
  public class SqlServerFixture
  {
    protected ISubscriptionStorage storage;
    protected ISubscriptionStorageSessionProvider subscriptionStorageSessionProvider;

    [Test, Explicit]
    public void CreateSqlSchema()
    {
      var sqlBuilder = new SqlConnectionStringBuilder();
      sqlBuilder.DataSource = "(local)";
      sqlBuilder.InitialCatalog = "nservicebus";
      sqlBuilder.IntegratedSecurity = true;

      var cfg = new Configuration()

        .DataBaseIntegration(x =>
                               {
                                 x.Dialect<MsSql2008Dialect>();
                                 x.ConnectionString = sqlBuilder.ConnectionString;
                               });

      var mapper = new ModelMapper();
      mapper.AddMappings(typeof(NHibernate.Config.SubscriptionMap).Assembly.GetExportedTypes());
      HbmMapping faultMappings = mapper.CompileMappingForAllExplicitlyAddedEntities();

      cfg.AddMapping(faultMappings);

      File.WriteAllText("schema.sql", "");

      new SchemaExport(cfg).Create(x => File.AppendAllText("schema.sql", x), true);

      subscriptionStorageSessionProvider = new SubscriptionStorageSessionProvider(cfg.BuildSessionFactory());

      storage = new SubscriptionStorage(subscriptionStorageSessionProvider);
    }
  }
}