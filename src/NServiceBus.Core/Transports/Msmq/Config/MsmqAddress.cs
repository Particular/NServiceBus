namespace NServiceBus.Msmq
{
    using System;
    using System.Net;
    using NServiceBus.Support;

    ///<summary>
    /// Abstraction for an MSMQ address on the NServiceBus network.
    ///</summary>
    public class MsmqAddress
    {
        /// <summary>
        /// Parses a string and returns an Address.
        /// </summary>
        /// <param name="destination">The full address to parse.</param>
        /// <returns>A new instance of <see cref="Address"/>.</returns>
        public static MsmqAddress Parse(string destination)
        {
            if (string.IsNullOrEmpty(destination))
            {
                throw new ArgumentNullException("destination");
            }

            var arr = destination.Split('@');

            var queue = arr[0];
            if (String.IsNullOrWhiteSpace(queue))
            {
                throw new ArgumentException("No destination address specified", "destination");
            }
            var machine = GetMachineName(arr);

            return new MsmqAddress(queue, machine);
        }

        static string GetMachineName(string[] arr)
        {
            if (arr.Length == 2)
            {
                var machineName = arr[1];
                if (machineName != "." && machineName.ToLower() != "localhost" && machineName != IPAddress.Loopback.ToString())
                {
                    if (String.IsNullOrWhiteSpace(machineName))
                    {
                        throw new ArgumentException("No machineName specified", "destination");
                    }
                    return machineName;
                }
            }
            return RuntimeEnvironment.MachineName;
        }

        /// <summary>
        /// Instantiate a new Address for a known queue on a given machine.
        /// </summary>
        ///<param name="queueName">The queue name.</param>
        ///<param name="machineName">The machine name.</param>
        public MsmqAddress(string queueName, string machineName)
        {
            if (string.IsNullOrWhiteSpace(queueName))
            {
                throw new ArgumentNullException("queueName");
            }
            if (string.IsNullOrWhiteSpace(machineName))
            {
                throw new ArgumentNullException("machineName");
            }
            Queue = queueName;
            Machine = machineName;
        }


        /// <summary>
        /// Returns a string representation of the address.
        /// </summary>
        public override string ToString()
        {
            return Queue + "@" + Machine;
        }

        /// <summary>
        /// The name of the queue not including the name of the machine or location depending on the address mode.
        /// </summary>
        public string Queue { get; private set; }

        /// <summary>
        /// The name of the machine or the (normal) name of the location depending on the address mode.
        /// </summary>
        public string Machine { get; private set; }


        /// <summary>
        /// Creates a new Address whose Queue is derived from the Queue of the existing Address
        /// together with the provided qualifier. For example: queue.qualifier@machine
        /// </summary>
        public MsmqAddress SubScope(string qualifier)
        {
            return new MsmqAddress(Queue + "." + qualifier, Machine);
        }


        /// <summary>
        /// Overloading for the == for the class Address
        /// </summary>
        /// <param name="left">Left hand side of == operator</param>
        /// <param name="right">Right hand side of == operator</param>
        /// <returns>true if the LHS is equal to RHS</returns>
        public static bool operator ==(MsmqAddress left, MsmqAddress right)
        {
            return Equals(left, right);
        }

        /// <summary>
        /// Overloading for the != for the class Address
        /// </summary>
        /// <param name="left">Left hand side of != operator</param>
        /// <param name="right">Right hand side of != operator</param>
        /// <returns>true if the LHS is not equal to RHS</returns>
        public static bool operator !=(MsmqAddress left, MsmqAddress right)
        {
            return !Equals(left, right);
        }

        /// <summary>
        /// Determines whether the specified <see cref="Object"/> is equal to the current <see cref="Object"/>.
        /// </summary>
        /// <returns>
        /// true if the specified <see cref="Object"/> is equal to the current <see cref="Object"/>; otherwise, false.
        /// </returns>
        /// <param name="obj">The <see cref="Object"/> to compare with the current <see cref="Object"/>. </param><filterpriority>2</filterpriority>
        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != typeof(MsmqAddress)) return false;
            return Equals((MsmqAddress)obj);
        }

        /// <summary>
        /// Check this is equal to other Address
        /// </summary>
        /// <param name="other">reference addressed to be checked with this</param>
        /// <returns>true if this is equal to other</returns>
        private bool Equals(MsmqAddress other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return string.Equals(Queue, other.Queue, StringComparison.OrdinalIgnoreCase) &&
                   string.Equals(Machine, other.Machine, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Provides a hash code of the Address.
        /// </summary>
        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = Queue.ToLowerInvariant().GetHashCode() * 397;
                hashCode ^= Machine.ToLowerInvariant().GetHashCode();
                return hashCode;
            }
        }
    }
}