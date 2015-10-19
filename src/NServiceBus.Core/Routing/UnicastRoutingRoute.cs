namespace NServiceBus.Routing
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    /// Represents a route in unicast routing.
    /// </summary>
    public sealed class UnicastRoutingRoute
    {
        UnicastRoutingDestination ultimateDestination;
        DirectRoutingImmediateDestination immediateDestination;

        /// <summary>
        /// Creates new instance of the route.
        /// </summary>
        /// <param name="ultimateDestination">The ultimate destination of the message.</param>
        /// <param name="immediateDestination">Optional immediate destination.</param>
        public UnicastRoutingRoute(UnicastRoutingDestination ultimateDestination, DirectRoutingImmediateDestination immediateDestination = null)
        {
            this.ultimateDestination = ultimateDestination;
            this.immediateDestination = immediateDestination ?? DirectRoutingImmediateDestination.None();
            Guard.AgainstNull(nameof(ultimateDestination), ultimateDestination);
        }

        internal IEnumerable<UnicastRoutingStrategy> Resolve(Func<EndpointName, IEnumerable<EndpointInstanceData>> instanceResolver,
            Func<IEnumerable<EndpointInstanceName>, IEnumerable<EndpointInstanceName>> instanceSelector,
            Func<EndpointInstanceName, string> addressResolver
            )
        {
            return ultimateDestination.Resolve(instanceResolver, instanceSelector, addressResolver)
                .Select(d => new UnicastRoutingStrategy(d.Item1, d.Item2, immediateDestination.Resolve(addressResolver)));
        }

        bool Equals(UnicastRoutingRoute other)
        {
            return Equals(ultimateDestination, other.ultimateDestination) && Equals(immediateDestination, other.immediateDestination);
        }

        /// <summary>
        /// Determines whether the specified object is equal to the current object.
        /// </summary>
        /// <returns>
        /// true if the specified object  is equal to the current object; otherwise, false.
        /// </returns>
        /// <param name="obj">The object to compare with the current object. </param>
        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj))
            {
                return false;
            }
            if (ReferenceEquals(this, obj))
            {
                return true;
            }
            return obj is UnicastRoutingRoute && Equals((UnicastRoutingRoute) obj);
        }

        /// <summary>
        /// Serves as a hash function for a particular type. 
        /// </summary>
        /// <returns>
        /// A hash code for the current object.
        /// </returns>
        public override int GetHashCode()
        {
            unchecked
            {
                return ((ultimateDestination != null ? ultimateDestination.GetHashCode() : 0)*397) ^ (immediateDestination != null ? immediateDestination.GetHashCode() : 0);
            }
        }

        /// <summary>
        /// Checks for equality.
        /// </summary>
        public static bool operator ==(UnicastRoutingRoute left, UnicastRoutingRoute right)
        {
            return Equals(left, right);
        }

        /// <summary>
        /// Checks for inequality.
        /// </summary>
        public static bool operator !=(UnicastRoutingRoute left, UnicastRoutingRoute right)
        {
            return !Equals(left, right);
        }
    }
}