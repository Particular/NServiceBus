namespace NServiceBus.Timeout.Tests
{
    using System;
    using System.Linq;
    using Core;
    using NUnit.Framework;

    [TestFixture]
    public class When_fetching_timeouts_from_storage : WithRavenTimeoutPersister
    {
        [Test]
        public void Should_return_the_complete_list_of_timeouts()
        {
            const int numberOfTimeoutsToAdd = 10;

            for (var i = 0; i < numberOfTimeoutsToAdd; i++)
            {
                persister.Add(new TimeoutData
                {
                    Time = DateTime.UtcNow.AddHours(1)
                });
            }

            Assert.AreEqual(numberOfTimeoutsToAdd, persister.GetAll().Count());
        }

        [Test]
        public void Should_only_return_timeouts_for_this_specific_endpoint_and_any_ones_without_a_owner()
        {
            const int numberOfTimeoutsToAdd = 3;
            
            for (var i = 0; i < numberOfTimeoutsToAdd; i++)
            {
                var d = new TimeoutData
                            {
                                Time = DateTime.UtcNow.AddHours(1),
                                OwningTimeoutManager = Configure.EndpointName
                            };

                persister.Add(d);
            }

            persister.Add(new TimeoutData
            {
                Time = DateTime.UtcNow.AddHours(1),
                OwningTimeoutManager = "MyOtherTM"
            });


            persister.Add(new TimeoutData
            {
                Time = DateTime.UtcNow.AddHours(1),
            });


            Assert.AreEqual(numberOfTimeoutsToAdd+1, persister.GetAll().Count());
        }

        [Test]
        public void Should_return_the_complete_list_of_timeouts_without_hitting_the_maximum_number_of_requests_allowed_for_this_session_has_been_reached()
        {
            var numberOfTimeoutsToAdd = (store.Conventions.MaxNumberOfRequestsPerSession + 1) * 1024;

            for (var i = 0; i < numberOfTimeoutsToAdd; i++)
            {
                persister.Add(new TimeoutData
                {
                    Time = DateTime.UtcNow.AddHours(1)
                });
            }

            Assert.AreEqual(numberOfTimeoutsToAdd, persister.GetAll().Count());
        }
    }
}
