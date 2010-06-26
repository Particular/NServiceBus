using System.IO;
using FluentNHibernate.Cfg.Db;
using NHibernate;
using NHibernate.ByteCode.LinFu;
using NHibernate.Cfg;
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
         var nhibernateProperties = SQLiteConfiguration.Standard
             .UsingFile(Path.GetTempFileName())
             .ProxyFactoryFactory(typeof(ProxyFactoryFactory).AssemblyQualifiedName)
             .ToProperties();

         FaultManagerSessionFactory factory =
            ConfigureNHibernateFaultManager.CreateSessionFactory(new Configuration().Configure().SetProperties(nhibernateProperties),true);

         SessionFactory = factory.Value;
         FaultManager = new FaultManager(factory);
      }
   }
}
