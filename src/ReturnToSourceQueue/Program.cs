namespace ReturnToSourceQueue
{
    using System;
    using System.Linq;
    using System.Net;
    using NServiceBus.Tools.Management.Errors.ReturnToSourceQueue;
    using NServiceBus.Transports.Msmq;

    class Program
    {
        static void Main(string[] args)
        {
            var errorManager = new ErrorManager();

            string inputQueue = null;
            string messageId = null;

            if (args != null && args.Length > 0)
                inputQueue = args[0];

            if (args != null && args.Length > 1)
                messageId = args[1];

            var script = true;

            if (inputQueue == null)
            {
                Console.WriteLine("NServiceBus ReturnToSource for MSMQ");
                Console.WriteLine("by Particular Software Ltd. \n");

                Console.WriteLine("Please enter the error queue you would like to use:");
                inputQueue = Console.ReadLine();
                if (string.IsNullOrWhiteSpace(inputQueue))
                {
                    Console.WriteLine("No error queue specified");
                    Console.WriteLine("\nPress 'Enter' to exit.");
                    Console.ReadLine();
                    return;
                }
                script = false;
            }

            var errorQueueAddress = MsmqAddress.Parse(inputQueue);

            if(!IsLocalIpAddress(errorQueueAddress.Machine))
            {
                Console.WriteLine("Input queue [{0}] resides on a remote machine: [{1}].", errorQueueAddress.Queue, errorQueueAddress.Machine);
                Console.WriteLine("Due to networking load, it is advised to refrain from using ReturnToSourceQueue on a remote error queue, unless the error queue resides on a clustered machine.");
                if (!script)
                {
                    Console.WriteLine(
                        "Press 'y' if the error queue resides on a Clustered Machine, otherwise press any key to exit.");
                    if (Console.ReadKey().Key.ToString().ToLower() != "y")
                        return;
                }
                Console.WriteLine(string.Empty);
                errorManager.ClusteredQueue = true;
            }
            
            if (messageId == null)
            {
                Console.WriteLine("Please enter the id of the message you'd like to return to its source queue, or 'all' to do so for all messages in the queue.");
                messageId = Console.ReadLine();
            }

            errorManager.InputQueue = errorQueueAddress;
            Console.WriteLine("Attempting to return message to source queue. Queue: [{0}], message id: [{1}]. Please stand by.",
                errorQueueAddress, messageId);

            try
            {
                if (messageId == "all")
                    errorManager.ReturnAll();
                else
                    errorManager.ReturnMessageToSourceQueue(messageId);

                if (args == null || args.Length == 0)
                {
                    Console.WriteLine("Press 'Enter' to exit.");
                    Console.ReadLine();
                }
            }
            catch(Exception e)
            {
                Console.WriteLine("Could not return message to source queue. Reason: " + e.Message);
                Console.WriteLine(e.StackTrace);

                Console.WriteLine("\nPress 'Enter' to exit.");
                Console.ReadLine();
            }
        }
        public static bool IsLocalIpAddress(string host)
        {
            // get host IP addresses
            var hostIPs = Dns.GetHostAddresses(host);
            // get local IP addresses
            var localIPs = Dns.GetHostAddresses(Dns.GetHostName());

            // test if any host IP equals to any local IP or to localhost
            foreach (var hostIP in hostIPs)
            {
                // is localhost
                if (IPAddress.IsLoopback(hostIP)) return true;
                // is local address
                if (localIPs.Contains(hostIP)) return true;
            }
            return false;
        }
    }
}
