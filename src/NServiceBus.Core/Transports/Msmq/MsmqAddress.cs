namespace NServiceBus.Transports.Msmq
{
    using System;
    using System.Net;
    using NServiceBus.Support;

    ///<summary>
    /// Abstraction for an address on the NServiceBus network.
    ///</summary>
    internal struct MsmqAddress
    {

        /// <summary>
        /// The (lowercase) name of the queue not including the name of the machine or location depending on the address mode.
        /// </summary>
        public readonly string Queue;

        /// <summary>
        /// The (lowercase) name of the machine or the (normal) name of the location depending on the address mode.
        /// </summary>
        public readonly string Machine;

        /// <summary>
        /// Parses a string and returns an Address.
        /// </summary>
        /// <param name="address">The full address to parse.</param>
        /// <returns>A new instance of <see cref="Address"/>.</returns>
        public static MsmqAddress Parse(string address)
        {
            Guard.AgainstNullAndEmpty(address, "address");

            var split = address.Split('@');

            if (split.Length > 2)
            {
                var message = string.Format("Address contains multiple @ characters. Address supplied: '{0}'", address);
                throw new ArgumentException(message, "address");
            }

            var queue = split[0];
            if (string.IsNullOrWhiteSpace(queue))
            {
                var message = string.Format("Empty queue part of address. Address supplied: '{0}'", address);
                throw new ArgumentException(message, "address");
            }

            string machineName;
            if (split.Length == 2)
            {
                machineName = split[1];
                if (string.IsNullOrWhiteSpace(machineName))
                {
                    var message = string.Format("Empty machine part of address. Address supplied: '{0}'", address);
                    throw new ArgumentException(message,"address");
                }
                machineName = ApplyLocalMachineConventions(machineName);
            }
            else
            {
                machineName = RuntimeEnvironment.MachineName;
            }

            return new MsmqAddress(queue, machineName);
        }

        static string ApplyLocalMachineConventions(string machineName)
        {
            if (
                machineName == "." || 
                machineName.ToLower() == "localhost" || 
                machineName == IPAddress.Loopback.ToString()
                )
            {
                return RuntimeEnvironment.MachineName;
            }
            return machineName;
        }

        /// <summary>
        /// Instantiate a new Address for a known queue on a given machine.
        /// </summary>
        ///<param name="queueName">The queue name.</param>
        ///<param name="machineName">The machine name.</param>
        public MsmqAddress(string queueName, string machineName)
        {
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
        /// Returns a string representation of the address.
        /// </summary>
        public string ToString(string qualifier)
        {
            return Queue + "." + qualifier + "@" + Machine;
        }

    }
}
