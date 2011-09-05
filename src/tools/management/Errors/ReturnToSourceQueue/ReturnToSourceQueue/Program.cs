using System;
using NServiceBus;
using NServiceBus.Tools.Management.Errors.ReturnToSourceQueue;

namespace ReturnToSourceQueue
{
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

            if (inputQueue == null)
            {
                Console.WriteLine("Please enter the error queue you would like to use:");
                inputQueue = Console.ReadLine();
            }

            if (messageId == null)
            {
                Console.WriteLine("Please enter the id of the message you'd like to return to its source queue, or 'all' to do so for all messages in the queue.");
                messageId = Console.ReadLine();

                Console.WriteLine("Attempting to return message to source queue. Please stand by.");
            }

            errorManager.InputQueue = Address.Parse(inputQueue);

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
    }
}
