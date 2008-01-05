using System;
using Common.Logging;
using NServiceBus;
using Messages;

namespace Client
{
    class Program
    {
        static void Main()
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

                bClient.Send(m).Register(UpdateCustomerComplete, m);
            }
        }

        private static void UpdateCustomerComplete(IAsyncResult asyncResult)
        {
            CompletionResult result = asyncResult.AsyncState as CompletionResult;

            if (result != null)
            {
                UpdateCustomerMessage m = result.state as UpdateCustomerMessage;
                if (m == null)
                    return;

                if (result.errorCode == (int)ErrorCode.None)
                    Console.WriteLine("Customer {0} updated successfully", m.CustomerId);
                if (result.errorCode == (int)ErrorCode.NotFound)
                    Console.WriteLine("Could not updated customer {0} - not found.", m.CustomerId);
            }
        }
    }
}
