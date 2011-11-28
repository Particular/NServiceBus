namespace MyServer
{
    using System;
    using NServiceBus;

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
            Bus.SendLocal(new StartSagaMessage());
            Console.WriteLine("Saga start message sent"); 
        }

       
        public void Stop()
        {
        }
    }
}