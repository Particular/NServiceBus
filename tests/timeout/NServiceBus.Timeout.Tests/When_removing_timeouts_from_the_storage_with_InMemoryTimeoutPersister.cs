namespace NServiceBus.Timeout.Tests
{
    using System.Linq;
    using System.Transactions;
    using Core;
    using NUnit.Framework;

    [TestFixture, Ignore]
    public class When_removing_timeouts_from_the_storage_with_InMemoryTimeoutPersister : WithInMemoryTimeoutPersister
    {
        [Test]
        public void Should_remove_timeouts_by_id()
        {
            var t1 = new TimeoutData();

            persister.Add(t1);

            var t2 = new TimeoutData();

            persister.Add(t2);

            var timeouts = GetNextChunk();

            foreach (var timeout in timeouts)
            {
                using (var tx = new TransactionScope())
                {
                    TimeoutData timeoutData;
                    persister.TryRemove(timeout.Item1, out timeoutData);

                    tx.Complete();
                }
            }

            timeouts = GetNextChunk();

            Assert.AreEqual(0, timeouts.Count());
        }
    }
}