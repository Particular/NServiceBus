using System;
using NServiceBus;
using TestMessage;

namespace TestClient
{
    public class ClientEndpoint : IWantToRunAtStartup
    {
        public IBus Bus { get; set;  }

        #region IWantToRunAtStartup Members

        public void Run()
        {            
            while (true)
            {
                Console.WriteLine("\n\nEnter ID: ");
                String id = Console.ReadLine();

                Console.WriteLine("Enter Name: ");
                String name = Console.ReadLine();

                var msg = new FtpMessage();
                msg.ID = Int32.Parse(id);
                msg.Name = name;

                
                this.Bus.Send(msg).Register<FtpReply>(t => Console.WriteLine("Reply Received: " + t.OtherData.ToString()));
                Console.ReadLine();                
            }

            
        }

        public void Stop()
        {
            
        }

        #endregion
    }
}
