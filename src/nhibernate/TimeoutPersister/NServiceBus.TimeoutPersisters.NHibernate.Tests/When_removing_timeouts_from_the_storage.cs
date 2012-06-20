namespace NServiceBus.TimeoutPersisters.NHibernate.Tests
{
    using System;
    using System.Collections.Generic;
    using NUnit.Framework;
    using Timeout.Core;

    [TestFixture]
    public class When_removing_timeouts_from_the_storage : InMemoryDBFixture
    {
        [Test]
        public void Should_remove_timeouts_by_id()
        {
            var t1 = new TimeoutData { Time = DateTime.Now.AddYears(1), OwningTimeoutManager = Configure.EndpointName, Headers = new Dictionary<string, string> { { "Header1", "Value1" } } };
            var t2 = new TimeoutData { Time = DateTime.Now.AddYears(1), OwningTimeoutManager = Configure.EndpointName, Headers = new Dictionary<string, string> { { "Header1", "Value1" } } };

            persister.Add(t1);
            persister.Add(t2);

            var t = persister.GetAll();

            foreach (var timeoutData in t)
            {
                persister.Remove(timeoutData.Id);
            }

            using (var session = sessionFactory.OpenSession())
            {
                Assert.Null(session.Get<TimeoutEntity>(new Guid(t1.Id)));
                Assert.Null(session.Get<TimeoutEntity>(new Guid(t2.Id)));
            }
        }

        [Test]
        public void Should_remove_timeouts_by_sagaid()
        {
            var sagaId1 = Guid.NewGuid();
            var sagaId2 = Guid.NewGuid();
            var t1 = new TimeoutData { SagaId = sagaId1, Time = DateTime.Now.AddYears(1), OwningTimeoutManager = Configure.EndpointName, Headers = new Dictionary<string, string> { { "Header1", "Value1" } } };
            var t2 = new TimeoutData { SagaId = sagaId2, Time = DateTime.Now.AddYears(1), OwningTimeoutManager = Configure.EndpointName, Headers = new Dictionary<string, string> { { "Header1", "Value1" } } };

            persister.Add(t1);
            persister.Add(t2);


            persister.ClearTimeoutsFor(sagaId1);
            persister.ClearTimeoutsFor(sagaId2);
            

            using (var session = sessionFactory.OpenSession())
            {
                Assert.Null(session.Get<TimeoutEntity>(new Guid(t1.Id)));
                Assert.Null(session.Get<TimeoutEntity>(new Guid(t2.Id)));
            }
        }
    }
}