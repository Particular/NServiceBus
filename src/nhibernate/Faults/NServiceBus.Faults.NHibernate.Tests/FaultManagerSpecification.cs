using System.IO;
using NHibernate;
using NHibernate.Cfg;
using NHibernate.Dialect;
using NUnit.Framework;

namespace NServiceBus.Faults.NHibernate.Tests
{
   public abstract class FaultManagerSpecification
   {
      protected FaultManager FaultManager;
      protected ISessionFactory SessionFactory;

      [SetUp]
      public void SetUp()
      {

        var cfg = new Configuration()
          .DataBaseIntegration(x =>
                                 {
                                   x.Dialect<SQLiteDialect>();
                                   x.ConnectionString = string.Format(@"Data Source={0};Version=3;New=True;", Path.GetTempFileName());
                                 });

         //var nhibernateProperties = SQLiteConfiguration.Standard
         //    .UsingFile(Path.GetTempFileName())
         //    .ProxyFactoryFactory(typeof(ProxyFactoryFactory).AssemblyQualifiedName)
         //    .ToProperties();

         //FaultManagerSessionFactory factory =
         //   ConfigureNHibernateFaultManager.CreateSessionFactory(new Configuration().Configure().SetProperties(nhibernateProperties),true);

        FaultManagerSessionFactory factory = ConfigureNHibernateFaultManager.CreateSessionFactory(cfg, true);

         SessionFactory = factory.Value;
         FaultManager = new FaultManager(factory);
      }
   }
}
