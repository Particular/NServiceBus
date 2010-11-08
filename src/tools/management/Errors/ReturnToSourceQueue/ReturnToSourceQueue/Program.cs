using System;
using NServiceBus.Tools.Management.Errors.ReturnToSourceQueue;

namespace ReturnToSourceQueue
{
    class Program
    {
        static void Main(string[] args)
        {
            ErrorManager c = new ErrorManager();

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

            c.InputQueue = inputQueue;

            try
            {
                if (messageId == "all")
                    c.ReturnAll();
                else
                    c.ReturnMessageToSourceQueue(messageId);

                Console.WriteLine("Success.");

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
            }
        }
    }
}
