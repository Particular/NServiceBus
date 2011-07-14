using System;
using System.Transactions;
using NServiceBus.Unicast.Transport;
using NUnit.Framework;

namespace NServiceBus.Faults.NHibernate.Tests
{
   [TestFixture]
   public class When_handling_faults_on_transactional_endpoints : FaultManagerSpecification
   {
      [Test]
      public void Ambient_transaction_should_commit_saving_failure_info()
      {
         using (var transactionScope = new TransactionScope())
         {
             FaultManager.SerializationFailedForMessage(new TransportMessage { ReplyToAddress = Address.Parse("returnAddress") }, new Exception());            
            transactionScope.Complete();
         }
         using (var session = SessionFactory.OpenSession())
         {
            Assert.AreEqual(session.CreateCriteria(typeof(FailureInfo)).List<FailureInfo>().Count,1);
         }
      }

      [Test]
      public void Ambient_transaction_should_rollback_saving_failure_info()
      {
         using (var transactionScope = new TransactionScope())
         {
             FaultManager.SerializationFailedForMessage(new TransportMessage { ReplyToAddress = Address.Parse("returnAddress") }, new Exception());
         }

         using (var session = SessionFactory.OpenSession())
         {
            Assert.AreEqual(session.CreateCriteria(typeof(FailureInfo)).List<FailureInfo>().Count,0);
         }

      }

   }
}