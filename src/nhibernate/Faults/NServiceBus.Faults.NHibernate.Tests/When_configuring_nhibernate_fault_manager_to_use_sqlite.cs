using NUnit.Framework;

namespace NServiceBus.Faults.NHibernate.Tests
{
   [TestFixture]
   public class When_configuring_nhibernate_fault_manager_to_use_sqlite
   {
      private Configure config;

      [SetUp]
      public void SetUp()
      {
         config = Configure.With(new[] { typeof(When_configuring_nhibernate_fault_manager_to_use_sqlite).Assembly })
            .DefaultBuilder()
            .NHibernateFaultManager(true);
      }

      [Test]
      public void Fault_manager_should_be_registered_as_singleton()
      {
         var persister = config.Builder.Build<FaultManager>();

         Assert.AreEqual(persister,config.Builder.Build<FaultManager>());
      }

      [Test]
      public void The_sessionfactory_should_be_built_and_registered_as_singleton()
      {
         var sessionFactory = config.Builder.Build<FaultManagerSessionFactory>();

         Assert.NotNull(sessionFactory);
         Assert.AreEqual(sessionFactory,config.Builder.Build<FaultManagerSessionFactory>());

      }      
   }
}