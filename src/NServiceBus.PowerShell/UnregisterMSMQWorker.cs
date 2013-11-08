namespace NServiceBus.PowerShell
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Management.Automation;
    using System.Messaging;
    using System.Net;
    using System.Xml.Serialization;

    [Cmdlet(VerbsLifecycle.Unregister, "NServiceBusMSMQWorker")]
    public class UnregisterMSMQWorker : PSCmdlet
    {
        [ValidateNotNullOrEmpty]
        [Parameter(Mandatory = true, Position = 0, HelpMessage = "The address of the Worker that accepts the dispatched message.", ValueFromPipeline = true)]
        public string WorkerAddress { get; set; }

        [ValidateNotNullOrEmpty]
        [Parameter(Mandatory = true, Position = 1, HelpMessage = "The Distributor address.", ValueFromPipeline = true)]
        public string DistributorAddress { get; set; }

        protected override void ProcessRecord()
        {
            var queueAddress = GetFullPath(DistributorAddress) + ".distributor.control";

            using (var queue = new MessageQueue(queueAddress))
            {
                var message = new Message
                {
                    Recoverable = true,
                };

                var headers = new Dictionary<string, string>
                {
                    {"NServiceBus.ControlMessage", true.ToString()},
                    {"NServiceBus.Distributor.UnregisterWorker", WorkerAddress}
                };

                var headerSerializer = new XmlSerializer(typeof(List<HeaderInfo>));
                using (var stream = new MemoryStream())
                {
                    headerSerializer.Serialize(stream, headers.Select(pair => new HeaderInfo
                    {
                        Key = pair.Key,
                        Value = pair.Value
                    }).ToList());
                    message.Extension = stream.ToArray();
                }
                if (queue.Transactional)
                {
                    // Create a transaction.
                    using (var transaction = new MessageQueueTransaction())
                    {
                        transaction.Begin();

                        queue.Send(message, transaction);

                        transaction.Commit();
                    }
                }
                else
                {
                    queue.Send(message);
                }
            }
        }

        static string GetFullPath(string address)
        {
            var arr = address.Split('@');

            var queue = arr[0];
            var machine = Environment.MachineName;

            if (arr.Length == 2)
            {
                if (arr[1] != "." && arr[1].ToLower() != "localhost" && arr[1] != IPAddress.Loopback.ToString())
                {
                    machine = arr[1];
                }
            }

            IPAddress ipAddress;
            if (IPAddress.TryParse(machine, out ipAddress))
            {
                return PREFIX_TCP + GetFullPathWithoutPrefix(queue, machine);
            }

            return PREFIX + GetFullPathWithoutPrefix(queue, machine);
        }

        static string GetFullPathWithoutPrefix(string queue, string machine)
        {
            return machine + PRIVATE + queue;
        }

        const string DIRECTPREFIX = "DIRECT=OS:";
        const string PRIVATE = "\\private$\\";
        const string DIRECTPREFIX_TCP = "DIRECT=TCP:";
        const string PREFIX_TCP = "FormatName:" + DIRECTPREFIX_TCP;
        const string PREFIX = "FormatName:" + DIRECTPREFIX;
    }
}