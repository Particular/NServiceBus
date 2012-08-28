namespace NServiceBus.Timeout.Tests
{
    using System;
    using System.Linq;
    using Core;
    using NUnit.Framework;

    [TestFixture, Ignore]
    public class When_fetching_timeouts_from_storage : WithRavenTimeoutPersister
    {
        [Test]
        public void Should_only_return_timeouts_for_time_slice()
        {
            const int numberOfTimeoutsToAdd = 10;

            for (var i = 0; i < numberOfTimeoutsToAdd; i++)
            {
                persister.Add(new TimeoutData
                {
                    OwningTimeoutManager = String.Empty,
                    Time = DateTime.UtcNow.AddHours(-1)
                });
            }

            for (var i = 0; i < numberOfTimeoutsToAdd; i++)
            {
                persister.Add(new TimeoutData
                {
                    OwningTimeoutManager = String.Empty,
                    Time = DateTime.UtcNow.AddHours(1)
                });
            }
            
            Assert.AreEqual(numberOfTimeoutsToAdd, GetNextChunk().Count());
        }

        [Test]
        public void Should_only_return_timeouts_for_this_specific_endpoint_and_any_ones_without_a_owner()
        {
            const int numberOfTimeoutsToAdd = 3;
            
            for (var i = 0; i < numberOfTimeoutsToAdd; i++)
            {
                var d = new TimeoutData
                            {
                                Time = DateTime.UtcNow.AddHours(-1),
                                OwningTimeoutManager = Configure.EndpointName
                            };

                persister.Add(d);
            }

            persister.Add(new TimeoutData
            {
                Time = DateTime.UtcNow.AddHours(-1),
                OwningTimeoutManager = "MyOtherTM"
            });


            persister.Add(new TimeoutData
            {
                Time = DateTime.UtcNow.AddHours(-1),
            });


            Assert.AreEqual(numberOfTimeoutsToAdd + 1, GetNextChunk().Count());
        }

        [Test]
        public void Should_return_all_timeouts_that_expire_now_and_set_the_next_run_to_now()
        {
            const int numberOfTimeoutsToAdd = 500;

            for (var i = 0; i < numberOfTimeoutsToAdd; i++)
            {
                var d = new TimeoutData
                {
                    Time = DateTime.UtcNow.AddHours(-1),
                    OwningTimeoutManager = Configure.EndpointName
                };

                persister.Add(d);
            }

            DateTime nextTimeToRunQuery;
            persister.GetNextChunk(out nextTimeToRunQuery);

            Assert.True((DateTime.UtcNow - nextTimeToRunQuery).TotalMilliseconds < 200);
        }
    }
}