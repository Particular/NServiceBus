namespace NServiceBus.Distributor.NHibernate.Tests
{
    using NUnit.Framework;

    [TestFixture]
    public class When_reseting_worker_availability : InMemoryDBFixture
    {
        [Test]
        public void Should_return_zero_workers_available()
        {
            var address = new Address("myqueue", "john1");
            persister.WorkerAvailable(address, 10);

            persister.ClearAvailabilityForWorker(address);

            Assert.IsNull(persister.PopAvailableWorker());
        }

        [Test]
        public void Should_only_clear_resetted_worker_availability_for_the_resetted_address()
        {
            var address1 = new Address("myqueue", "john1");
            persister.WorkerAvailable(address1, 10);
            var address2 = new Address("myqueue", "john2");
            persister.WorkerAvailable(address2, 10);

            persister.ClearAvailabilityForWorker(address1);

            Assert.IsNotNull(persister.PopAvailableWorker());
        }

        [Test]
        public void Should_reset_all_workers_for_the_resetted_address()
        {
            var address1 = new Address("myqueue", "john1");
            persister.WorkerAvailable(address1, 10);
            var address2 = new Address("myqueue", "john2");
            persister.WorkerAvailable(address2, 10);

            persister.ClearAvailabilityForWorker(address1);

            Address currentWorkerAddress;

            while ((currentWorkerAddress = persister.PopAvailableWorker()) != null)
            {
                Assert.AreNotEqual(address1, currentWorkerAddress);
            }
        }
    }
}