﻿namespace NServiceBus.Routing
{
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    /// A routing strategy for unicast routing.
    /// </summary>
    public class UnicastRoutingStrategy : RoutingStrategy
    {
        string ultimateDestination;
        string[] route;

        /// <summary>
        /// Creates new routing strategy.
        /// </summary>
        public UnicastRoutingStrategy(string ultimateDestination, params string[] route)
        {
            this.ultimateDestination = ultimateDestination;
            this.route = route;
        }

        /// <summary>
        /// Returns a new routing strategy that first routes the message to <paramref name="next"/>.
        /// </summary>
        /// <param name="next">The immediate destination.</param>
        public UnicastRoutingStrategy SendVia(string next)
        {
            return new UnicastRoutingStrategy(ultimateDestination, new[] { next}.Concat(route).ToArray());
        }

        /// <summary>
        /// Applies the routing strategy to the message.
        /// </summary>
        /// <param name="headers">Message headers.</param>
        public override AddressTag Apply(Dictionary<string, string> headers)
        {
            var itinerary = new Itinerary(ultimateDestination, route);
            string immediateDestination;
            var newItinerary = itinerary.Advance(out immediateDestination);
            newItinerary.Store(headers);
            return new UnicastAddressTag(immediateDestination);
        }
    }
}