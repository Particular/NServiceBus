namespace NServiceBus.Timeout.Tests
{
    using System.Linq;
    using Core;
    using NUnit.Framework;

    [TestFixture]
    public class When_fetching_all_timeouts_from_storage : WithRavenTimeoutPersister
    {
        [Test]
        public void Should_return_the_complete_list_of_timeouts()
        {
            const int timeoutsToAdd = 2000;

            for (int i = 0; i < timeoutsToAdd; i++)
            {
                persister.Add(new TimeoutData());
                
            }
            Assert.AreEqual(timeoutsToAdd,persister.GetAll().Count());


        }
    }
}