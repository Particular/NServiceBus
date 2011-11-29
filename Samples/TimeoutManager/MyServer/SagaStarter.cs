namespace MyServer
{
    using System;
    using NServiceBus;
    using Saga;

    class SagaStarter:IWantToRunAtStartup
    {
        public IBus Bus { get; set; }

        public void Run()
        {
            Console.WriteLine("Press 'S' to start the saga");
            Console.WriteLine("To exit, press Ctrl + C");

            string cmd;

            while ((cmd = Console.ReadKey().Key.ToString().ToLower()) != "q")
            {
                switch (cmd)
                {
                    case "s":
                        StartSaga();
                        break;
                }
            }
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