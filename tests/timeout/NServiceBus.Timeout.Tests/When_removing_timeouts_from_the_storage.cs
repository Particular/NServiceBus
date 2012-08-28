namespace NServiceBus.Timeout.Tests
{
    using System;
    using System.Linq;
    using System.Transactions;
    using Core;
    using NUnit.Framework;

    [TestFixture, Ignore]
    public class When_removing_timeouts_from_the_storage : WithRavenTimeoutPersister
    {
        [Test]
        public void Should_remove_timeouts_by_id()
        {
            using (var tx = new TransactionScope())
            {
                var t1 = new TimeoutData {Time = DateTime.UtcNow.AddHours(-1)};
                persister.Add(t1);
                tx.Complete();
            }

            using (var tx = new TransactionScope())
            {
                var t2 = new TimeoutData {Time = DateTime.UtcNow.AddHours(-1)};
                persister.Add(t2);
                tx.Complete();
            }

            var timouts = GetNextChunk().ToList();

            foreach (var timeoutData in timouts)
            {
                using (var tx = new TransactionScope())
                {
                    persister.TryRemove(timeoutData.Id);
                    tx.Complete();
                }
            }

            timouts = GetNextChunk().ToList();
            
            Assert.AreEqual(0, timouts.Count);
        }
    }
}