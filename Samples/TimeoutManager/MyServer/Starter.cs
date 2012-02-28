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
            Console.WriteLine("Press 'T' to start the saga in multi tennant mode");
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

                    case "t":
                        //make sure that the database exists!
                        StartSaga("MyApp.Tennants.Acme");
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

        void StartSaga(string tennant = "")
        {
            var message = new StartSagaMessage
                              {
                                  OrderId = Guid.NewGuid()
                              };
            if (!string.IsNullOrEmpty(tennant))            
                message.SetHeader("tennant", tennant);
            
                
            Bus.SendLocal(message);
            Console.WriteLine(string.Format("{0} - {1}", DateTime.Now.ToLongTimeString(), "Saga start message sent")); 
        }

       
        public void Stop()
        {
        }
    }
}