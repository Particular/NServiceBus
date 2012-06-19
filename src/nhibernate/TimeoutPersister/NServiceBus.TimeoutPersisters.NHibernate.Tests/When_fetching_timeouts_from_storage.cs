namespace NServiceBus.TimeoutPersisters.NHibernate.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using NUnit.Framework;
    using Timeout.Core;

    [TestFixture]
    public class When_fetching_timeouts_from_storage : InMemoryDBFixture
    {
        [Test]
        public void Should_return_the_complete_list_of_timeouts()
        {
            const int numberOfTimeoutsToAdd = 10;

            for (var i = 0; i < numberOfTimeoutsToAdd; i++)
            {
                persister.Add(new TimeoutData
                                  {
                                      Time = DateTime.UtcNow.AddHours(1),
                                      CorrelationId = "boo",
                                      Destination = new Address("timeouts", Environment.MachineName),
                                      SagaId = Guid.NewGuid(),
                                      State = new byte[] { 0, 0, 133 },
                                      Headers = new Dictionary<string, string> { { "Bar", "34234" }, { "Foo", "dasdsa" }, { "Super", "dsfsdf" }},
                                      OwningTimeoutManager = Configure.EndpointName,
                                  });
            }

            Assert.AreEqual(numberOfTimeoutsToAdd, persister.GetAll().Count());
        }

        [Test]
        public void Should_return_the_correct_headers()
        {
            const int numberOfTimeoutsToAdd = 10;
            var headers = new Dictionary<string, string> { { "Bar", "34234" }, { "Foo", "dasdsa" }, { "Super", "dsfsdf" } };

            for (var i = 0; i < numberOfTimeoutsToAdd; i++)
            {
                persister.Add(new TimeoutData
                {
                    Time = DateTime.UtcNow.AddHours(1),
                    CorrelationId = "boo",
                    Destination = new Address("timeouts", Environment.MachineName),
                    SagaId = Guid.NewGuid(),
                    State = new byte[] { 1, 1, 133, 200 },
                    Headers = headers,
                    OwningTimeoutManager = Configure.EndpointName,
                });
            }

            var timeouts = persister.GetAll();
            foreach (var timeoutData in timeouts)
            {
                CollectionAssert.AreEqual(headers, timeoutData.Headers);
            }
        }
    }
}