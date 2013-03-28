namespace NServiceBus.Transports.Msmq
{
    using System;
    using System.Net;

    /// <summary>
    /// 
    /// </summary>
    public class MsmqAddress : Address, AddressParser
    {
        static readonly string DefaultMachine = Environment.MachineName;

        public MsmqAddress()
        {
            
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="queueName"></param>
        /// <param name="machineName"></param>
        public MsmqAddress(string queueName, string machineName)
        {
            Queue = queueName.ToLower();
            Machine = machineName.ToLower();
        }
        /// <summary>
        /// The (lowercase) name of the queue not including the name of the machine or location depending on the address mode.
        /// </summary>
        public string Queue { get; set; }

        /// <summary>
        /// The (lowercase) name of the machine or the (normal) name of the location depending on the address mode.
        /// </summary>
        public string Machine { get; set; }

        public override Address SubScope(string qualifier)
        {
            return new MsmqAddress(Queue + "." + qualifier.ToLower(), Machine);
        }

        public override string Name
        {
            get { return Queue; }
        }

        public override string FullName
        {
            get { return Queue + "@" + Machine; }
        }

        Address AddressParser.Parse(string destination)
        {
            if (string.IsNullOrEmpty(destination))
            {
                throw new ArgumentException("Invalid destination address specified", "destination");
            }

            var arr = destination.Split('@');

            var queue = arr[0];
            var machine = DefaultMachine;

            if (String.IsNullOrWhiteSpace(queue))
            {
                throw new ArgumentException("Invalid destination address specified", "destination");
            }

            if (arr.Length == 2)
                if (arr[1] != "." && arr[1].ToLower() != "localhost" && arr[1] != IPAddress.Loopback.ToString())
                    machine = arr[1];

            return new MsmqAddress(queue, machine);
        }
    }
}