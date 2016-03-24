namespace NServiceBus
{
    using System;
    using System.Net;
    using Support;

    struct MsmqAddress
    {
        public readonly string Queue;

        public readonly string Machine;

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
                    throw new ArgumentException(message, nameof(address));
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

        public MsmqAddress(string queueName, string machineName)
        {
            Queue = queueName;
            Machine = machineName;
        }

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