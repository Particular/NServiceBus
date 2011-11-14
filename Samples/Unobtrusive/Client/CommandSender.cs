namespace Client
{
    using System;
    using Commands;
    using Messages;
    using NServiceBus;

    class CommandSender:IWantToRunAtStartup
    {
        public IBus Bus { get; set; }

        public void Run()
        {
            Console.WriteLine("Press 'C' to send a command");
            Console.WriteLine("Press 'R' to send a request");
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
                }
            }
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
            })
            .Register<CommandStatus>(outcome=> Console.WriteLine("Server returned status: " + outcome));

            Console.WriteLine("Command sent id: " + commandId);
            
        }

        public void Stop()
        {
        }
    }
}