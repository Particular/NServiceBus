namespace NServiceBus
{
    using System;
    using System.Net;
    using System.Runtime.Serialization;
    using System.Security;
    using Support;

    ///<summary>
    /// Abstraction for an address on the NServiceBus network.
    ///</summary>
    [Serializable]
    public class Address : ISerializable
    {
        /// <summary>
        /// Undefined address.
        /// </summary>
        public static readonly Address Undefined = new Address(String.Empty, String.Empty);

        /// <summary>
        /// Self address.
        /// </summary>
        public static readonly Address Self = new Address("__self", "localhost");

        

        /// <summary>
        /// Get the address of this endpoint.
        /// </summary>
        public static Address Local { get; private set; }


        /// <summary>
        /// Sets the address of this endpoint.
        /// </summary>
        /// <param name="queue">The queue name.</param>
        public static void InitializeLocalAddress(string queue)
        {
            Local = Parse(queue);
            PublicReturnAddress = Local;

            if (preventChanges)
                throw new InvalidOperationException("Overwriting a previously set local address is a very dangerous operation. If you think that your scenario warrants it, you can catch this exception and continue.");
        }

        internal static Address PublicReturnAddress { get; private set; }

        /// <summary>
        /// Sets the public return address of this endpoint.
        /// </summary>
        /// <param name="address">The public address.</param>
        public static void OverridePublicReturnAddress(Address address)
        {
            PublicReturnAddress = address;

            if (preventChanges)
                throw new InvalidOperationException("Overwriting a previously set public return address is a very dangerous operation. If you think that your scenario warrants it, you can catch this exception and continue.");
        }

        /// <summary>
        /// Sets the address mode, can only be done as long as the local address is not been initialized.By default the default machine equals Environment.MachineName
        /// </summary>
        /// <param name="machineName">The machine name.</param>
        public static void OverrideDefaultMachine(string machineName)
        {
            defaultMachine = machineName;

            if (preventChanges)
                throw new InvalidOperationException("Overwriting a previously set default machine name is a very dangerous operation. If you think that your scenario warrants it, you can catch this exception and continue.");
        }

        /// <summary>
        /// Instructed the address to not consider the machine name
        /// </summary>
        public static void IgnoreMachineName()
        {
            ignoreMachineName = true;
        }

        /// <summary>
        /// Parses a string and returns an Address.
        /// </summary>
        /// <param name="destination">The full address to parse.</param>
        /// <returns>A new instance of <see cref="Address"/>.</returns>
        public static Address Parse(string destination)
        {
            if (string.IsNullOrEmpty(destination))
            {
                throw new ArgumentException("Invalid destination address specified", "destination");
            }

            var arr = destination.Split('@');

            var queue = arr[0];
            var machine = defaultMachine;

            if (String.IsNullOrWhiteSpace(queue))
            {
                throw new ArgumentException("Invalid destination address specified", "destination");
            }

            if (arr.Length == 2)
                if (arr[1] != "." && arr[1].ToLower() != "localhost" && arr[1] != IPAddress.Loopback.ToString())
                    machine = arr[1];

            return new Address(queue, machine);
        }

        /// <summary>
        /// Instantiate a new Address for a known queue on a given machine.
        /// </summary>
        ///<param name="queueName">The queue name.</param>
        ///<param name="machineName">The machine name.</param>
        public Address(string queueName, string machineName)
        {
            Queue = queueName;
            queueLowerCased = queueName.ToLower();
            Machine = machineName ?? defaultMachine;
            machineLowerCased = Machine.ToLower();
        }

        /// <summary>
        /// Deserializes an Address.
        /// </summary>
        /// <param name="info">The <see cref="SerializationInfo"/> to populate with data. </param>
        /// <param name="context">The destination (see <see cref="StreamingContext"/>) for this serialization. </param>
        protected Address(SerializationInfo info, StreamingContext context)
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
        public Address SubScope(string qualifier)
        {
            return new Address(Queue + "." + qualifier, Machine);
        }

        /// <summary>
        /// Provides a hash code of the Address.
        /// </summary>
        public override int GetHashCode()
        {
            unchecked
            {
                return ((queueLowerCased != null ? queueLowerCased.GetHashCode() : 0) * 397) ^ (machineLowerCased != null ? machineLowerCased.GetHashCode() : 0);
            }
        }

        /// <summary>
        /// Returns a string representation of the address.
        /// </summary>
        public override string ToString()
        {
            if (ignoreMachineName)
                return Queue;

            return Queue + "@" + Machine;
        }

        /// <summary>
        /// Prevents changes to all addresses.
        /// </summary>
        public static void PreventChanges()
        {
            preventChanges = true;
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
        public static bool operator ==(Address left, Address right)
        {
            return Equals(left, right);
        }

        /// <summary>
        /// Overloading for the != for the class Address
        /// </summary>
        /// <param name="left">Left hand side of != operator</param>
        /// <param name="right">Right hand side of != operator</param>
        /// <returns>true if the LHS is not equal to RHS</returns>
        public static bool operator !=(Address left, Address right)
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
            if (obj.GetType() != typeof(Address)) return false;
            return Equals((Address)obj);
        }

        /// <summary>
        /// Check this is equal to other Address
        /// </summary>
        /// <param name="other">reference addressed to be checked with this</param>
        /// <returns>true if this is equal to other</returns>
        private bool Equals(Address other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;

            if (!ignoreMachineName && !other.machineLowerCased.Equals(machineLowerCased))
                return false;

            return other.queueLowerCased.Equals(queueLowerCased);

        }

        static string defaultMachine = RuntimeEnvironment.MachineName;
        static bool preventChanges;

        readonly string queueLowerCased;
        readonly string machineLowerCased;
        static bool ignoreMachineName;

    }
}
