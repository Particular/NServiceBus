namespace NServiceBus
{
    /// <summary>
    /// 
    /// </summary>
    public sealed class Destination
    {
        Destination()
        {
        }

        /// <summary>
        /// 
        /// </summary>
        public static readonly Destination ThisEndpoint = new Destination
        {
            Option = RouteOption.RouteToAnyInstanceOfThisEndpoint
        };

        /// <summary>
        /// 
        /// </summary>
        public static readonly Destination ThisInstance = new Destination
        {
            Option = RouteOption.RouteToThisInstance
        };

        /// <summary>
        /// 
        /// </summary>
        /// <param name="instanceId"></param>
        /// <returns></returns>
        public static Destination SpecificInstance(string instanceId)
        {
            return new Destination
            {
                Option = RouteOption.RouteToSpecificInstance,
                Value = instanceId
            };
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="address"></param>
        /// <returns></returns>
        public static Destination Address(string address)
        {
            return new Destination
            {
                Option = RouteOption.ExplicitDestination,
                Value = address
            };
        }

        internal string Value { get; private set; }

        internal RouteOption Option { get; private set; }

        internal enum RouteOption
        {
            ExplicitDestination,
            RouteToThisInstance,
            RouteToAnyInstanceOfThisEndpoint,
            RouteToSpecificInstance
        }

        bool Equals(Destination other)
        {
            return string.Equals(Value, other.Value) && Option == other.Option;
        }

        /// <summary>
        /// Determines whether the specified object is equal to the current object.
        /// </summary>
        /// <returns>
        /// true if the specified object  is equal to the current object; otherwise, false.
        /// </returns>
        /// <param name="obj">The object to compare with the current object. </param><filterpriority>2</filterpriority>
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
            return obj is Destination && Equals((Destination) obj);
        }

        /// <summary>
        /// Serves as the default hash function. 
        /// </summary>
        /// <returns>
        /// A hash code for the current object.
        /// </returns>
        /// <filterpriority>2</filterpriority>
        public override int GetHashCode()
        {
            unchecked
            {
                return ((Value != null ? Value.GetHashCode() : 0)*397) ^ (int) Option;
            }
        }

        /// <summary>
        /// </summary>
        public static bool operator ==(Destination left, Destination right)
        {
            return Equals(left, right);
        }

        /// <summary>
        /// </summary>
        public static bool operator !=(Destination left, Destination right)
        {
            return !Equals(left, right);
        }
    }
}