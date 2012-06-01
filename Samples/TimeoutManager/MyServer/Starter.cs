using MyServer.Scheduling;

namespace MyServer
{
    using System;
    using System.Collections.Concurrent;
    using DeferedProcessing;
    using NServiceBus;
    using PerformanceTest;
    using Saga;

    class Starter:IWantToRunAtStartup
    {
        public IBus Bus { get; set; }

        public void Run()
        {
            Console.WriteLine("Press 'S' to start the saga");
            Console.WriteLine("Press 'T' to start the saga in multi tenant mode");
            Console.WriteLine("Press 'D' to defer a message 10 seconds");
            Console.WriteLine("Press 'R' to schedule a task");
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
                        StartSaga("MyApp.Tenants.Acme");
                        break;

                    case "d":
                        DeferMessage();
                        break;

                    case "r":
                        ScheduleTask();
                        break;
                    case "p":
                        PerformanceTest();
                        break;
                }
            }
        }

        void PerformanceTest()
        {
            var total = 40000;
            PerformanceTestMessageHandler.receivedMessages = new ConcurrentBag<string>();
            PerformanceTestMessageHandler.NumExpectedMessages = total;
            PerformanceTestMessageHandler.TimeStarted = DateTime.UtcNow;
            System.Threading.Tasks.Parallel.For(0, total, _ => Bus.Defer(TimeSpan.FromMinutes(20), new PerformanceTestMessage()));
        }

        void DeferMessage()
        {
            Console.WriteLine(string.Format("{0} - {1}", DateTime.Now.ToLongTimeString(), "Sending a message to be processed at a later time"));
            Bus.SendLocal(new DeferredMessage
                              {
                                  ProcessAt = DateTime.Now.AddSeconds(10)
                              });
        }

        void StartSaga(string tenant = "")
        {
            var message = new StartSagaMessage
                              {
                                  OrderId = Guid.NewGuid()
                              };
            if (!string.IsNullOrEmpty(tenant))            
                message.SetHeader("tenant", tenant);
            
                
            Bus.SendLocal(message);
            Console.WriteLine(string.Format("{0} - {1}", DateTime.Now.ToLongTimeString(), "Saga start message sent")); 
        }
       
        void ScheduleTask()
        {            
            // The actual scheduling is done in ScheduleATaskHandler
            Bus.SendLocal(new ScheduleATask());
        }

        public void Stop()
        {
        }
    }
}