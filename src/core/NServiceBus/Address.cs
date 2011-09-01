using System;
using System.Net;
using System.Runtime.Serialization;

namespace NServiceBus
{
    ///<summary>
    /// Abstraction for an address on the NServiceBus network.
    ///</summary>
    [Serializable]
    public class Address : ISerializable
    {
        private static AddressMode addressMode = AddressMode.Local;
        private static string defaultMachine = Environment.MachineName;

        /// <summary>
        /// Get the address of this endpoint.
        /// </summary>
        public static Address Local { get; private set; }

        /// <summary>
        /// Sets the address of this endpoint.
        /// Will throw an exception if overwriting a previous value (but value will still be set).
        /// </summary>
        /// <param name="queue"></param>
        public static void InitializeLocalAddress(string queue)
        {
            if (Local == null)
                Local = Parse(queue);
            else
                throw new InvalidOperationException("Overwriting a previously set local address is a very dangerous operation. If you think that your scenario warrants it, you can catch this exception and continue.");
        }

        /// <summary>
        /// Sets the address mode, can only be done as long as the local address is not been initialized.By default the default machine equals Environment.MachineName
        /// </summary>
        /// <param name="machineName"></param>
        /// <exception cref="InvalidOperationException"></exception>
        public static void OverrideDefaultMachine(string machineName)
        {
            if (Local != null)
                throw new InvalidOperationException("The local address has already been initialized, changing the default machine name is no longer possible.");

            defaultMachine = machineName;
        }

        /// <summary>
        /// Sets the name of the machine to be used when none is specified in the address.
        /// </summary>
        /// <param name="mode"></param>
        /// <exception cref="InvalidOperationException"></exception>
        public static void InitializeAddressMode(AddressMode mode)
        {
            if (Local != null)
                throw new InvalidOperationException("The local address has already been initialized, switching address modes is no longer possible.");

            addressMode = mode;
        }

        /// <summary>
        /// Parses a string and returns an Address.
        /// </summary>
        /// <param name="destination"></param>
        /// <returns></returns>
        public static Address Parse(string destination)
        {
            if(string.IsNullOrEmpty(destination))
                throw new InvalidOperationException("Invalid destination address specified");

            var arr = destination.Split('@');

            var queue = arr[0];
            var machine = defaultMachine;

            if (arr.Length == 2)
                if (arr[1] != "." && arr[1].ToLower() != "localhost" && arr[1] != IPAddress.Loopback.ToString())
                    machine = arr[1];

            return new Address(queue, machine);
        }

        ///<summary>
        /// Instantiate a new Address for a known queue on a given machine.
        ///</summary>
        ///<param name="queueName"></param>
        ///<param name="machineName"></param>
        public Address(string queueName, string machineName)
        {
            Queue = queueName.ToLower();
            Machine = addressMode == AddressMode.Local ? machineName.ToLower() : machineName;
        }

        /// <summary>
        /// Deserializes an Address.
        /// </summary>
        /// <param name="info"></param>
        /// <param name="context"></param>
        protected Address(SerializationInfo info, StreamingContext context)
        {
            Queue = info.GetString("Queue");
            Machine = info.GetString("Machine");
        }

        void ISerializable.GetObjectData(SerializationInfo info, StreamingContext context)
        {
            info.AddValue("Queue", Queue);
            info.AddValue("Machine", Machine);
        }

        /// <summary>
        /// Implicit cast from string to Address.
        /// </summary>
        /// <param name="s"></param>
        /// <returns></returns>
        public static implicit operator Address(string s)
        {
            return Parse(s);
        }

        /// <summary>
        /// Implicit cast from Address to string.
        /// </summary>
        /// <param name="a"></param>
        /// <returns></returns>
        public static implicit operator string(Address a)
        {
            return a == null ? null : a.ToString();
        }

        /// <summary>
        /// Creates a new Address whose Queue is derived from the Queue of the existing Address
        /// together with the provided qualifier. For example: queue.qualifier@machine
        /// </summary>
        /// <param name="qualifier"></param>
        /// <returns></returns>
        public Address SubScope(string qualifier)
        {
            return new Address(Queue + "." + qualifier, Machine);
        }

        /// <summary>
        /// Checks equality with another Address
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public override bool Equals(object obj)
        {
            var address = obj as Address;
            if (address != null)
                return (Queue == address.Queue && Machine == address.Machine);

            return false;
        }

        /// <summary>
        /// Provides a hash code of the Address.
        /// </summary>
        /// <returns></returns>
        public override int GetHashCode()
        {
            return this.ToString().GetHashCode();
        }

        /// <summary>
        /// Returns a string representation of the address.
        /// </summary>
        /// <returns></returns>
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
    }
}
