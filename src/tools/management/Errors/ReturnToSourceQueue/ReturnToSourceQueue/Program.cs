
using System;
using NServiceBus.Tools.Management.Errors.ReturnToSourceQueue;
using Common.Logging;

namespace ReturnToSourceQueue
{
    class Program
    {
        static void Main(string[] args)
        {
            Class1 c = new Class1();

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
                Console.WriteLine("Please enter the id of the message you'd like to return to its source queue.");
                messageId = Console.ReadLine();

                Console.WriteLine("Attempting to return message to source queue. Please stand by.");
            }

            c.InputQueue = inputQueue;

            try
            {
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
                Console.WriteLine("Could not return message to source queue.");
                LogManager.GetLogger("ReturnToSourceQueue").Debug("Could not return message to source queue.", e);
            }
        }
    }
}
