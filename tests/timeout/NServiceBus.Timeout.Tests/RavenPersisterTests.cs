namespace NServiceBus.Timeout.Tests
{
    using System.Transactions;
    using Core;
    using Hosting.Windows.Persistence;
    using NUnit.Framework;
    using Raven.Client;
    using Raven.Client.Embedded;

    [TestFixture]
    public class RavenPersisterTests : WithRavenTimeoutPersister
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
                    //other tx stuff like pop a message from MSMQ

                    persister.Remove(timeoutData.Id);

                    tx.Complete();
                }
            }

            using (var session = store.OpenSession())
            {
                session.Advanced.AllowNonAuthoritativeInformation = false;

                Assert.Null(session.Load<TimeoutData>(t1.Id));
                Assert.Null(session.Load<TimeoutData>(t2.Id));
            }


        }
    }

    public class WithRavenTimeoutPersister
    {
        protected IPersistTimeouts persister;
        protected IDocumentStore store;

        [TestFixtureSetUp]
        public void SetupContext()
        {
            store = new EmbeddableDocumentStore { RunInMemory = true };
            //store = new DocumentStore { Url = "http://localhost:8080", DefaultDatabase = "MyServer" };

            store.Initialize();

            persister = new RavenTimeoutPersistence(store);
        }

    }
}