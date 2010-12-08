using System;
using System.ServiceModel;
using System.ServiceModel.Channels;
using Messages;

namespace Client
{
    internal class Program
    {
        private static readonly ChannelFactory<ICancelOrderService> ChannelFactory = new ChannelFactory<ICancelOrderService>("");

        private static void Main()
        {
            Console.WriteLine("This will send requests to the CancelOrder WCF service");
            Console.WriteLine("Press 'Enter' to send a message.To exit, Ctrl + C");

            ICancelOrderService client = ChannelFactory.CreateChannel();
            int orderId = 1;

            try
            {
                while (Console.ReadLine() != null)
                {
                    var message = new CancelOrder
                                  {
                                      OrderId = orderId++
                                  };

                    Console.WriteLine("Sending message with OrderId {0}.", message.OrderId);

                    ErrorCodes returnCode = client.Process(message);

                    Console.WriteLine("Error code returned: " + returnCode);
                }
            }
            finally
            {
                try
                {
                    ((IChannel) client).Close();
                }
                catch
                {
                    ((IChannel)client).Abort();
                }
            }
        }
    }
}