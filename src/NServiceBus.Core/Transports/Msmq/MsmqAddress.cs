namespace NServiceBus.Transports.Msmq
{
    using System;
    using System.Net;
    using NServiceBus.Support;

    ///<summary>
    /// Abstraction for an address on the NServiceBus network.
    ///</summary>
    public struct MsmqAddress
    {
        readonly string queue;
        readonly string machine;

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
            var machine = GetMachineName(arr);
            return new MsmqAddress(queue, machine);
        }

        static string GetMachineName(string[] arr)
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
            queue = queueName;
            machine = machineName ?? RuntimeEnvironment.MachineName;
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
        /// Returns a string representation of the address.
        /// </summary>
        public override string ToString()
        {
            return Queue + "@" + Machine;
        }

        /// <summary>
        /// The (lowercase) name of the queue not including the name of the machine or location depending on the address mode.
        /// </summary>
        public string Queue
        {
            get { return queue; }
        }

        /// <summary>
        /// The (lowercase) name of the machine or the (normal) name of the location depending on the address mode.
        /// </summary>
        public string Machine
        {
            get { return machine; }
        }
    }
}
