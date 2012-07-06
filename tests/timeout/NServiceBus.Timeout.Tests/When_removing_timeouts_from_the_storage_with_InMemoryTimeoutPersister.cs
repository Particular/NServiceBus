namespace NServiceBus.Timeout.Tests
{
    using System.Linq;
    using System.Transactions;
    using Core;
    using NUnit.Framework;

    [TestFixture]
    public class When_removing_timeouts_from_the_storage_with_InMemoryTimeoutPersister : WithInMemoryTimeoutPersister
    {
        [Test]
        public void Should_remove_timeouts_by_id()
        {
            var t1 = new TimeoutData();

            persister.Add(t1);

            var t2 = new TimeoutData();

            persister.Add(t2);

            var t = persister.GetAll();

            foreach (var timeoutData in t)
            {
                using (var tx = new TransactionScope())
                {
                    persister.Remove(timeoutData.Id);

                    tx.Complete();
                }
            }

            t = persister.GetAll();
            Assert.AreEqual(0, t.Count());
        }
    }
}