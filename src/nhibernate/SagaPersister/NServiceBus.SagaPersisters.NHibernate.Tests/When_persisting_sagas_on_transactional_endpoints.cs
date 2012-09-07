namespace NServiceBus.SagaPersisters.NHibernate.Tests
{
    using System;
    using System.Transactions;
    using NUnit.Framework;

    [TestFixture]
    public class When_persisting_sagas_on_transactional_endpoints : InMemoryFixture
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
                Assert.AreEqual(1, session.CreateCriteria(typeof(TestSaga)).List<TestSaga>().Count);
            }
        }

        [Test]
        public void Ambient_transaction_should_rollback_saga()
        {
            using (new TransactionScope())
            {
                UnitOfWork.Begin();

                SagaPersister.Save(new TestSaga
                    {
                        Id = Guid.NewGuid()
                    });
            }

            UnitOfWork.End(new Exception());
       
            using (var session = SessionFactory.OpenSession())
            {
                Assert.AreEqual(0, session.CreateCriteria(typeof(TestSaga)).List<TestSaga>().Count);
            }
        }
    }
}