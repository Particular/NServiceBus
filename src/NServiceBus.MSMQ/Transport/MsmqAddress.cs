namespace NServiceBus
{
    using System;
    using System.Net;
    using NServiceBus.Support;

    ///<summary>
    /// Abstraction for an address on the NServiceBus network.
    ///</summary>
    struct MsmqAddress
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
            Guard.AgainstNullAndEmpty(nameof(address), address);

            var split = address.Split('@');

            if (split.Length > 2)
            {
                var message = $"Address contains multiple @ characters. Address supplied: '{address}'";
                throw new ArgumentException(message, nameof(address));
            }

            var queue = split[0];
            if (string.IsNullOrWhiteSpace(queue))
            {
                var message = $"Empty queue part of address. Address supplied: '{address}'";
                throw new ArgumentException(message, nameof(address));
            }

            string machineName;
            if (split.Length == 2)
            {
                machineName = split[1];
                if (string.IsNullOrWhiteSpace(machineName))
                {
                    var message = $"Empty machine part of address. Address supplied: '{address}'";
                    throw new ArgumentException(message,nameof(address));
                }
                machineName = ApplyLocalMachineConventions(machineName);
            }
            else
            {
                machineName = RuntimeEnvironment.MachineName;
            }

            return new MsmqAddress(queue, machineName);
        }

        public bool IsRemote => Machine != RuntimeEnvironment.MachineName;

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
        /// Returns an equivalent address which is compatible with a given one with regards to how machine is specified. If a given address has machine specified via IP (as opposed
        /// to host name), the new address will also have machine specified via IP.
        /// </summary>
        /// <param name="other">The address to be compatible with.</param>
        /// <param name="ipLookup">The IP address lookup method.</param>
        /// <returns></returns>
        public MsmqAddress MakeCompatibleWith(MsmqAddress other, Func<string, string> ipLookup)
        {
            IPAddress _;
            if (IPAddress.TryParse(other.Machine, out _) && !IPAddress.TryParse(Machine, out _))
            {
                return new MsmqAddress(Queue, ipLookup(Machine));
            }
            return this;
        }

        public string FullPath
        {
            get
            {
                IPAddress ipAddress;
                if (IPAddress.TryParse(Machine, out ipAddress))
                {
                    return PREFIX_TCP + PathWithoutPrefix;
                }
                return PREFIX + PathWithoutPrefix;                
            }
        }

        public string PathWithoutPrefix => Machine + PRIVATE + Queue;

        /// <summary>
        /// Returns a string representation of the address.
        /// </summary>
        public override string ToString()
        {
            return Queue + "@" + Machine;
        }

        const string DIRECTPREFIX_TCP = "DIRECT=TCP:";
        const string PREFIX_TCP = "FormatName:" + DIRECTPREFIX_TCP;

        const string PREFIX = "FormatName:" + DIRECTPREFIX;
        const string DIRECTPREFIX = "DIRECT=OS:";

        const string PRIVATE = "\\private$\\";
    }
}
