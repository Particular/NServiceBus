namespace NServiceBus.TimeoutPersisters.NHibernate.Tests
{
    using System;
    using System.Collections.Generic;
    using NUnit.Framework;
    using Support;
    using Timeout.Core;

    [TestFixture]
    public class When_removing_timeouts_from_the_storage : InMemoryDBFixture
    {
        [Test]
        public void Should_return_the_correct_headers()
        {
            var headers = new Dictionary<string, string> { { "Bar", "34234" }, { "Foo", "dasdsa" }, { "Super", "dsfsdf" } };

            var timeout = new TimeoutData
                {
                    Time = DateTime.UtcNow.AddHours(-1), CorrelationId = "boo", Destination = new Address("timeouts", RuntimeEnvironment.MachineName), SagaId = Guid.NewGuid(), State = new byte[] {1, 1, 133, 200}, Headers = headers, OwningTimeoutManager = Configure.EndpointName,
                };
            persister.Add(timeout);

            TimeoutData timeoutData;
            persister.TryRemove(timeout.Id, out timeoutData);

            CollectionAssert.AreEqual(headers, timeoutData.Headers);
        }

        [Test]
        public void Should_remove_timeouts_by_id()
        {
            var t1 = new TimeoutData { Time = DateTime.Now.AddYears(-1), OwningTimeoutManager = Configure.EndpointName, Headers = new Dictionary<string, string> { { "Header1", "Value1" } } };
            var t2 = new TimeoutData { Time = DateTime.Now.AddYears(-1), OwningTimeoutManager = Configure.EndpointName, Headers = new Dictionary<string, string> { { "Header1", "Value1" } } };

            persister.Add(t1);
            persister.Add(t2);

            DateTime nextTimeToRunQuery;
            var timeouts = persister.GetNextChunk(DateTime.UtcNow.AddYears(-3), out nextTimeToRunQuery);

            foreach (var timeout in timeouts)
            {
                TimeoutData timeoutData;
                persister.TryRemove(timeout.Item1, out timeoutData);
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


            persister.RemoveTimeoutBy(sagaId1);
            persister.RemoveTimeoutBy(sagaId2);
            
            using (var session = sessionFactory.OpenSession())
            {
                Assert.Null(session.Get<TimeoutEntity>(new Guid(t1.Id)));
                Assert.Null(session.Get<TimeoutEntity>(new Guid(t2.Id)));
            }
        }
    }
}