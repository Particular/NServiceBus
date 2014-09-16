using System;
using System.ServiceModel;
using System.ServiceModel.Channels;
using Messages;

namespace Client
{
    class Program
    {
        private static readonly ChannelFactory<ICancelOrderService> ChannelFactory = new ChannelFactory<ICancelOrderService>("");

        private static void Main()
        {
            Console.WriteLine("This will send requests to the CancelOrder WCF service");
            Console.WriteLine("Press 'Enter' to send a message.To exit, Ctrl + C");

            var client = ChannelFactory.CreateChannel();
            var orderId = 1;

            try
            {
                while (Console.ReadLine() != null)
                {
                    var message = new CancelOrder
                                  {
                                      OrderId = orderId++
                                  };

                    Console.WriteLine("Sending message with OrderId {0}.", message.OrderId);

                    var returnCode = client.Process(message);

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