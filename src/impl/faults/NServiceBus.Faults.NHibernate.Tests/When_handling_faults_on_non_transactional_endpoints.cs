using System;
using NServiceBus.Unicast.Transport;
using NUnit.Framework;

namespace NServiceBus.Faults.NHibernate.Tests
{
   [TestFixture]
   public class When_handling_faults_on_non_transactional_endpoints : FaultManagerSpecification
   {
      [Test]
      public void Reporting_failure_should_commit_info_immediately()
      {
         FaultManager.SerializationFailedForMessage(new TransportMessage{ReturnAddress = "returnAddress"}, new Exception());

         using (var session = SessionFactory.OpenSession())
         {
            Assert.AreEqual(session.CreateCriteria(typeof(FailureInfo)).List<FailureInfo>().Count,1);
         }
      }
   }
}