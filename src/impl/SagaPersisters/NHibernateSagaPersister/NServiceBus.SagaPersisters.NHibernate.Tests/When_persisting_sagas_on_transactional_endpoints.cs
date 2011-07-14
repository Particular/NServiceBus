using System;
using System.Transactions;
using NUnit.Framework;

namespace NServiceBus.SagaPersisters.NHibernate.Tests
{
    [TestFixture]
    public class When_persisting_sagas_on_transactional_endpoints:InMemoryFixture
    {
        [Test]
        public void Ambient_transaction_should_commit_saga()
        {     
            using (var transactionScope = new TransactionScope())
            {
                UnitOfWork.Begin();
               
                SagaPersister.Save(new TestSaga
                                   {
                                       Id = Guid.NewGuid()
                                   });

                UnitOfWork.End();
                transactionScope.Complete();
            }
            using (var session = SessionFactory.OpenSession())
            {
                Assert.AreEqual(session.CreateCriteria(typeof(TestSaga)).List<TestSaga>().Count,1);
            }
 
        }



        [Test]
        public void Ambient_transaction_should_rollback_saga()
        {
            using (var transactionScope = new TransactionScope())
            {
                UnitOfWork.Begin();

                SagaPersister.Save(new TestSaga
                {
                    Id = Guid.NewGuid()
                });

            }

            UnitOfWork.Error();
       
            using (var session = SessionFactory.OpenSession())
            {
                Assert.AreEqual(session.CreateCriteria(typeof(TestSaga)).List<TestSaga>().Count,0);
            }

        }

    }

   
}