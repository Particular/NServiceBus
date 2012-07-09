namespace NServiceBus.Distributor.NHibernate.Tests
{
    using System.Collections.Generic;
    using NUnit.Framework;

    [TestFixture]
    public class When_fetching_workers : InMemoryDBFixture
    {
        [Test]
        public void Should_return_return_zero_workers_available_if_no_worker_added()
        {
            Assert.IsNull(persister.PopAvailableWorker());
        }

        [Test]
        public void Should_return_number_of_workers_added()
        {
            const int worker1Capacity = 50;
            const int worker2Capacity = 50;

            persister.WorkerAvailable(new Address("myqueue", "john1"), worker1Capacity);
            persister.WorkerAvailable(new Address("myqueue", "john2"), worker2Capacity);

            var result = 0;

            while (persister.PopAvailableWorker() != null)
            {
                result++;
            }
            
            Assert.AreEqual(worker1Capacity + worker2Capacity, result);
        }

        [Test]
        public void Should_return_workers_in_fifo_order()
        {
            var expectedWorker1Address = new Address("myqueue", "john1");
            persister.WorkerAvailable(expectedWorker1Address, 1);
            var expectedWorker2Address = new Address("myqueue", "john2");
            persister.WorkerAvailable(expectedWorker2Address, 1);
            var expectedWorker3Address = new Address("myqueue", "john3");
            persister.WorkerAvailable(expectedWorker3Address, 1);

            var result = persister.PopAvailableWorker();
            Assert.AreEqual(expectedWorker1Address, result);

            result = persister.PopAvailableWorker();
            Assert.AreEqual(expectedWorker2Address, result);

            result = persister.PopAvailableWorker();
            Assert.AreEqual(expectedWorker3Address, result);
        }

        [Test]
        public void Should_return_all_workers()
        {
            var expectedWorker1Address = new Address("myqueue", "john1");
            persister.WorkerAvailable(expectedWorker1Address, 5);
            var expectedWorker2Address = new Address("myqueue", "john2");
            persister.WorkerAvailable(expectedWorker2Address, 6);
            var expectedWorker3Address = new Address("myqueue", "john3");
            persister.WorkerAvailable(expectedWorker3Address, 2);

            persister.WorkerAvailable(expectedWorker1Address, 3);
            persister.WorkerAvailable(expectedWorker3Address, 2);

            var workersReturned = new Dictionary<Address, int>();
            Address currentWorkerAddress;
 
            while ((currentWorkerAddress = persister.PopAvailableWorker()) != null)
            {
                if (!workersReturned.ContainsKey(currentWorkerAddress))
                {
                    workersReturned[currentWorkerAddress] = 0;
                }

                workersReturned[currentWorkerAddress]++;
            }

            Assert.AreEqual(5 + 3, workersReturned[expectedWorker1Address]);
            Assert.AreEqual(6, workersReturned[expectedWorker2Address]);
            Assert.AreEqual(2 + 2, workersReturned[expectedWorker3Address]);
        }
    }
}
