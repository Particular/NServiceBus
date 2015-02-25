namespace NServiceBus.Transports.Msmq
{
    using System;
    using System.Net;
    using System.Runtime.Serialization;
    using System.Security;
    using NServiceBus.Support;

    ///<summary>
    /// Abstraction for an address on the NServiceBus network.
    ///</summary>
    [Serializable]
    public class MsmqAddress : ISerializable
    {
        /// <summary>
        /// Undefined address.
        /// </summary>
        public static readonly MsmqAddress Undefined = new MsmqAddress(String.Empty, String.Empty);

        /// <summary>
        /// Self address.
        /// </summary>
        public static readonly MsmqAddress Self = new MsmqAddress("__self", "localhost");

       
        /// <summary>
        /// Parses a string and returns an Address.
        /// </summary>
        /// <param name="destination">The full address to parse.</param>
        /// <returns>A new instance of <see cref="Address"/>.</returns>
        public static MsmqAddress Parse(string destination)
        {
            if (string.IsNullOrEmpty(destination))
            {
                throw new ArgumentException("Invalid destination address specified", "destination");
            }

            var arr = destination.Split('@');

            var queue = arr[0];
            if (String.IsNullOrWhiteSpace(queue))
            {
                throw new ArgumentException("Invalid destination address specified", "destination");
            }
            var machine = GetMachineName(arr, queue);
            return new MsmqAddress(queue, machine);
        }

        static string GetMachineName(string[] arr, string queue)
        {
            var machine=RuntimeEnvironment.MachineName;

            if (arr.Length == 2)
            {
                if (arr[1] != "." && arr[1].ToLower() != "localhost" && arr[1] != IPAddress.Loopback.ToString())
                {
                    machine = arr[1];
                }
            }

            return machine;
        }

        /// <summary>
        /// Instantiate a new Address for a known queue on a given machine.
        /// </summary>
        ///<param name="queueName">The queue name.</param>
        ///<param name="machineName">The machine name.</param>
        public MsmqAddress(string queueName, string machineName)
        {
            Queue = queueName;
            queueLowerCased = queueName.ToLower();
            Machine = machineName ?? RuntimeEnvironment.MachineName;
            machineLowerCased = Machine.ToLower();
        }

        /// <summary>
        /// Deserializes an Address.
        /// </summary>
        /// <param name="info">The <see cref="SerializationInfo"/> to populate with data. </param>
        /// <param name="context">The destination (see <see cref="StreamingContext"/>) for this serialization. </param>
        protected MsmqAddress(SerializationInfo info, StreamingContext context)
        {
            Queue = info.GetString("Queue");
            Machine = info.GetString("Machine");
            
            if (!String.IsNullOrEmpty(Queue))
            {
                queueLowerCased = Queue.ToLower();
            }

            if (!String.IsNullOrEmpty(Machine))
            {
                machineLowerCased = Machine.ToLower();
            }
        }

        /// <summary>
        /// Populates a <see cref="SerializationInfo"/> with the data needed to serialize the target object.
        /// </summary>
        /// <param name="info">The <see cref="SerializationInfo"/> to populate with data. </param>
        /// <param name="context">The destination (see <see cref="StreamingContext"/>) for this serialization. </param>
        /// <exception cref="SecurityException">The caller does not have the required permission. </exception>
        void ISerializable.GetObjectData(SerializationInfo info, StreamingContext context)
        {
            info.AddValue("Queue", Queue);
            info.AddValue("Machine", Machine);
        }

        /// <summary>
        /// Creates a new Address whose Queue is derived from the Queue of the existing Address
        /// together with the provided qualifier. For example: queue.qualifier@machine
        /// </summary>
        public MsmqAddress SubScope(string qualifier)
        {
            return new MsmqAddress(Queue + "." + qualifier, Machine);
        }

        /// <summary>
        /// Provides a hash code of the Address.
        /// </summary>
        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = ((queueLowerCased != null ? queueLowerCased.GetHashCode() : 0)*397);

                hashCode ^= (machineLowerCased != null ? machineLowerCased.GetHashCode() : 0);
                return hashCode;
            }
        }

        /// <summary>
        /// Returns a string representation of the address.
        /// </summary>
        public override string ToString()
        {
            return Queue + "@" + Machine;
        }

        /// <summary>
        /// The (lowercase) name of the queue not including the name of the machine or location depending on the address mode.
        /// </summary>
        public string Queue { get; private set; }

        /// <summary>
        /// The (lowercase) name of the machine or the (normal) name of the location depending on the address mode.
        /// </summary>
        public string Machine { get; private set; }

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

            if (!other.machineLowerCased.Equals(machineLowerCased))
                return false;

            return other.queueLowerCased.Equals(queueLowerCased);
        }

        readonly string queueLowerCased;
        readonly string machineLowerCased;
    }
}
