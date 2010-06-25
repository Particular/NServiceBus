using System;
using System.Transactions;
using NBehave.Spec.NUnit;
using NUnit.Framework;

namespace NServiceBus.SagaPersisters.NHibernate.Tests
{
    [TestFixture]
    public class When_persisting_sagas_on_non_transactional_endpoints : MessageModuleFixture
    {
        [Test]
        public void Successful_execution_should_commit_nhibernate_transaction()
        {
            UnitOfWork.Begin();

            SagaPersister.Save(new TestSaga
            {
                Id = Guid.NewGuid()
            });

            UnitOfWork.End();

            using (var session = SessionFactory.OpenSession())
            {
                session.CreateCriteria(typeof(TestSaga)).List<TestSaga>().Count.ShouldEqual(1);
            }
        }

        [Test]
        public void Error_should_rollback_nhibernate_transaction()
        {
            UnitOfWork.Begin();

            SagaPersister.Save(new TestSaga
            {
                Id = Guid.NewGuid()
            });
            UnitOfWork.Error();

            using (var session = SessionFactory.OpenSession())
            {
                session.CreateCriteria(typeof(TestSaga)).List<TestSaga>().Count.ShouldEqual(0);
            }
        }
    }
}