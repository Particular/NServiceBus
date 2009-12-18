using System;
using System.Transactions;
using NBehave.Spec.NUnit;
using NUnit.Framework;
using Rhino.Mocks;

namespace NServiceBus.SagaPersisters.NHibernate.Tests
{
    [TestFixture]
    public class When_persisting_sagas_on_transactional_endpoints:MessageModuleFixture
    {
        [Test]
        public void Ambient_transaction_should_commit_saga()
        {     
            using (var transactionScope = new TransactionScope())
            {
                MessageModule.HandleBeginMessage();
               
                SagaPersister.Save(new TestSaga
                                   {
                                       Id = Guid.NewGuid()
                                   });

                MessageModule.HandleEndMessage();
                transactionScope.Complete();
            }
            using (var session = SessionFactory.OpenSession())
            {
                session.CreateCriteria(typeof(TestSaga)).List<TestSaga>().Count.ShouldEqual(1);
            }
 
        }



        [Test]
        public void Ambient_transaction_should_rollback_saga()
        {
            using (var transactionScope = new TransactionScope())
            {
                MessageModule.HandleBeginMessage();

                SagaPersister.Save(new TestSaga
                {
                    Id = Guid.NewGuid()
                });

            }

            MessageModule.HandleError();
       
            using (var session = SessionFactory.OpenSession())
            {
                session.CreateCriteria(typeof(TestSaga)).List<TestSaga>().Count.ShouldEqual(0);
            }

        }

    }
}