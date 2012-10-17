namespace MyServer.Common
{
    using System;
    using System.Threading.Tasks;
    using NServiceBus;

    public class Application : IWantToRunAtStartup
    {
        public IBus Bus { get; set; }

        public void Run()
        {
            Console.WriteLine("Press 'S' to send a message that will throw an exception.");
            Console.WriteLine("Press 'Q' to exit.");

            string cmd;

            while ((cmd = Console.ReadKey().Key.ToString().ToLower()) != "q")
            {
                switch (cmd)
                {
                    case "s":

                        Parallel.For(0, 100, (s, r) =>
                            {
                                var m = new MyMessage {Id = Guid.NewGuid()};

                                Bus.Send(m);
                            });
              
                        break;
                }
            }
        }

        public void Stop()
        {            
        }
    }
}