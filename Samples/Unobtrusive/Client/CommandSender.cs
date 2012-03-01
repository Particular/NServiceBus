namespace Client
{
    using System;
    using Commands;
    using Messages;
    using NServiceBus;
    using MyMessages;

    class CommandSender:IWantToRunAtStartup
    {
        public IBus Bus { get; set; }

        public void Run()
        {
            Console.WriteLine("Press 'C' to send a command");
            Console.WriteLine("Press 'R' to send a request");
            Console.WriteLine("Press 'D' to send a deferred message");
            Console.WriteLine("Press 'S' to start the saga");
            
            Console.WriteLine("To exit, press Ctrl + C");

            string cmd;

            while ((cmd = Console.ReadKey().Key.ToString().ToLower()) != "q")
            {
                switch (cmd)
                {
                    case "c":
                        SendCommand();
                        break;
                    case "r":
                        SendRequest();
                        break;
                    case "d":
                        DeferMessage();
                        break;
                    case "s":
                        StartSaga();
                        break;


                }
            }
        }

        void StartSaga(string tennant = "")
        {
            var message = new StartSagaMessage
            {
                OrderId = Guid.NewGuid()
            };
            if (!string.IsNullOrEmpty(tennant))
                message.SetHeader("tennant", tennant);


            Bus.Send(message);
            Console.WriteLine(string.Format("{0} - {1}", DateTime.Now.ToLongTimeString(), "Saga start message sent"));
        }
        void DeferMessage()
        {
            Bus.Defer(TimeSpan.FromSeconds(10), new DeferredMessage());
            Console.WriteLine(string.Format("{0} - {1}", DateTime.Now.ToLongTimeString(), "Sent a message that is deferred for 10 seconds"));
        }
        void SendRequest()
        {
            var requestId = Guid.NewGuid();

            Bus.Send<Request>(m =>
            {
                m.RequestId = requestId;
            });
            Console.WriteLine("Request sent id: " + requestId); 
        }

        void SendCommand()
        {
            var commandId = Guid.NewGuid();

            Bus.Send<MyCommand>(m =>
            {
                m.CommandId = commandId;
                m.EncryptedString = "Some sensitive information";
            })
            .Register<CommandStatus>(outcome=> Console.WriteLine("Server returned status: " + outcome));

            Console.WriteLine("Command sent id: " + commandId);
            
        }

        public void Stop()
        {
        }
    }
}