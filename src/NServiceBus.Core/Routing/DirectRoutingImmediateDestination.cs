namespace NServiceBus.Routing
{
    using System;

    /// <summary>
    /// An immediate destination of direct routing.
    /// </summary>
    public sealed class DirectRoutingImmediateDestination
    {
        EndpointInstanceName instanceName;
        string physicalAddress;
        static string[] emptyRoute = new string[0];

        private DirectRoutingImmediateDestination()
        {
        }

        internal static DirectRoutingImmediateDestination None()
        {
            return new DirectRoutingImmediateDestination();
        }

        /// <summary>
        /// Creates a destination based on the name of the endpoint instance.
        /// </summary>
        /// <param name="instanceName">Destination instance name.</param>
        public DirectRoutingImmediateDestination(EndpointInstanceName instanceName)
        {
            Guard.AgainstNull(nameof(instanceName), instanceName);
            this.instanceName = instanceName;
        }

        /// <summary>
        /// Creates a destination based on the physical address.
        /// </summary>
        /// <param name="physicalAddress">Destination physical address.</param>
        public DirectRoutingImmediateDestination(string physicalAddress)
        {
            Guard.AgainstNullAndEmpty(nameof(physicalAddress), physicalAddress);
            this.physicalAddress = physicalAddress;
        }

        internal string[] Resolve(Func<EndpointInstanceName, string> addressResolver)
        {
            if (physicalAddress != null)
            {
                return new[] {physicalAddress};
            }
            if (instanceName != null)
            {
                return new []{ addressResolver(instanceName)};
            }
            return emptyRoute;
        }

        bool Equals(DirectRoutingImmediateDestination other)
        {
            return Equals(instanceName, other.instanceName) && string.Equals(physicalAddress, other.physicalAddress);
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
            return obj is DirectRoutingImmediateDestination && Equals((DirectRoutingImmediateDestination) obj);
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
                return ((instanceName != null ? instanceName.GetHashCode() : 0)*397) ^ (physicalAddress != null ? physicalAddress.GetHashCode() : 0);
            }
        }

        /// <summary>
        /// Checks for equality.
        /// </summary>
        public static bool operator ==(DirectRoutingImmediateDestination left, DirectRoutingImmediateDestination right)
        {
            return Equals(left, right);
        }

        /// <summary>
        /// Checks for inequality.
        /// </summary>
        public static bool operator !=(DirectRoutingImmediateDestination left, DirectRoutingImmediateDestination right)
        {
            return !Equals(left, right);
        }
    }
}