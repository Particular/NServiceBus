namespace MyServer
{
    using System;
    using DeferedProcessing;
    using NServiceBus;
    using Saga;

    class Starter:IWantToRunAtStartup
    {
        public IBus Bus { get; set; }

        public void Run()
        {
            Console.WriteLine("Press 'S' to start the saga");
            Console.WriteLine("Press 'D' to defer a message 10 seconds");
            Console.WriteLine("To exit, press Ctrl + C");

            string cmd;

            while ((cmd = Console.ReadKey().Key.ToString().ToLower()) != "q")
            {
                switch (cmd)
                {
                    case "s":
                        StartSaga();
                        break;

                    case "d":
                        DeferMessage();
                        break;
                }
            }
        }

        void DeferMessage()
        {
            Bus.Defer(TimeSpan.FromSeconds(10), new DeferredMessage());
            Console.WriteLine(string.Format("{0} - {1}", DateTime.Now.ToLongTimeString(), "Sent a message that is deferred for 10 seconds")); 
        }

        void StartSaga()
        {
            Bus.SendLocal(new StartSagaMessage
                              {
                                  OrderId = Guid.NewGuid()
                              });
            Console.WriteLine(string.Format("{0} - {1}", DateTime.Now.ToLongTimeString(), "Saga start message sent")); 
        }

       
        public void Stop()
        {
        }
    }
}