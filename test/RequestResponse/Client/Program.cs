using System;
using Common.Logging;
using NServiceBus;
using Messages;
using NServiceBus.Async;

namespace Client
{
    class Program
    {
        static void Main(string[] args)
        {
            LogManager.GetLogger("hello").Debug("Started.");
            ObjectBuilder.SpringFramework.Builder builder = new ObjectBuilder.SpringFramework.Builder();
                
            IBus bClient = builder.Build<IBus>();

            bClient.Start();

            bClient.Subscribe(typeof(CustomerUpdatedMessage));

            Console.WriteLine("Press 'Enter' to send a message. To exit, press 'q' and then 'Enter'.");
            while (Console.ReadLine().ToLower() != "q")
            {
                UpdateCustomerMessage m = new UpdateCustomerMessage();
                m.CustomerId = Guid.NewGuid();

                Console.WriteLine("Requesting to update customer {0}", m.CustomerId);

                bClient.Send(m, new CompletionCallback(UpdateCustomerComplete), m);
            }
        }

        private static void UpdateCustomerComplete(int errorCode, object state)
        {
            UpdateCustomerMessage m = state as UpdateCustomerMessage;
            if (m == null)
                return;

            if (errorCode == (int)ErrorCode.None)
                Console.WriteLine("Customer {0} updated successfully", m.CustomerId);
            if (errorCode == (int)ErrorCode.NotFound)
                Console.WriteLine("Could not updated customer {0} - not found.", m.CustomerId);
        }
    }
}
