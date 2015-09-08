namespace NServiceBus.Core.Tests.Routing
{
    using System.Collections.Generic;
    using NServiceBus.Routing;
    using NUnit.Framework;

    [TestFixture]
    class ItineraryTests
    {
        [Test]
        public void Empty_itinerary_is_empty()
        {
            var empty = Itinerary.Empty();
            Assert.IsTrue(empty.IsEmpty);
        }

        [Test]
        public void Advancing_an_empty_itinerary_retuns_an_empty_itinerary()
        {
            string immediateDestination;
            var empty = Itinerary.Empty();
            var advanced = empty.Advance(out immediateDestination);

            Assert.IsTrue(advanced.IsEmpty);
            Assert.IsNull(immediateDestination);
        }

        [Test]
        public void Advancing_an_itinerary_without_send_via_returns_an_empty_itinerary()
        {
            string immediateDestination;
            var itinerary = new Itinerary("Ultimate");
            var advanced = itinerary.Advance(out immediateDestination);

            Assert.IsTrue(advanced.IsEmpty);
            Assert.AreEqual("Ultimate", immediateDestination);
        }

        [Test]
        public void Advancing_an_itinerary_with_send_via_hops_returns_a_non_empty_itinerary()
        {
            string immediateDestination;
            var itinerary = new Itinerary("Ultimate","Hop-1","Hop-2");

            var advanced = itinerary.Advance(out immediateDestination);
            Assert.IsFalse(advanced.IsEmpty);
            Assert.AreEqual("Hop-1", immediateDestination);

            advanced = advanced.Advance(out immediateDestination);
            Assert.IsFalse(advanced.IsEmpty);
            Assert.AreEqual("Hop-2", immediateDestination);

            advanced = advanced.Advance(out immediateDestination);
            Assert.IsTrue(advanced.IsEmpty);
            Assert.AreEqual("Ultimate", immediateDestination);
        }

        [Test]
        public void Storing_empty_itinerary_is_noop()
        {
            var itinerary = Itinerary.Empty();
            var store = new Dictionary<string, string>();

            itinerary.Store(store);

            Assert.IsEmpty(store);
        }

        [Test]
        public void Storing_a_no_hop_itinerary_stores_only_ultimate_destination_header()
        {
            var itinerary = new Itinerary("Ultimate");
            var store = new Dictionary<string, string>();

            itinerary.Store(store);

            Assert.AreEqual(1, store.Count);
            Assert.AreEqual("Ultimate", store[Headers.UltimateDestination]);
        }

        [Test]
        public void Storing_a_multi_hop_itinerary_stores_the_send_via_headers()
        {
            var itinerary = new Itinerary("Ultimate", "Hop-1", "Hop-2");
            var store = new Dictionary<string, string>();

            itinerary.Store(store);

            Assert.AreEqual(3, store.Count);
            Assert.AreEqual("Ultimate", store[Headers.UltimateDestination]);
            Assert.AreEqual("Hop-1", store[Headers.SendVia + ".1"]);
            Assert.AreEqual("Hop-2", store[Headers.SendVia + ".2"]);
        }
    }
}
