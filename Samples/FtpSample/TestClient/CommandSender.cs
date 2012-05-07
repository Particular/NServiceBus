using System;
using NServiceBus;
using TestMessage;

namespace TestClient
{
    class CommandSender : IWantToRunAtStartup
    {
        readonly Random random = new Random();

        public IBus Bus { get; set; }

        public void Run()
        {
            Console.WriteLine("Press 'R' to send a request");
            Console.WriteLine("To exit, press Ctrl + C");

            while (true)
            {
                var cmd = Console.ReadKey().Key.ToString().ToLower();
                switch (cmd)
                {
                    case "r":
                        SendRequest();
                        break;
                }
            }
        }

        void SendRequest()
        {
            var msg = new FtpMessage {Id = random.Next(1000), Name = "John Smith"};

            Bus.Send(msg);

            Console.WriteLine("Request sent id: " + msg.Id); 
        }

        public void Stop()
        {
        }
    }
}