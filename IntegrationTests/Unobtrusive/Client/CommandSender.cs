namespace Client
{
    using System;
    using Commands;
    using Messages;
    using NServiceBus;

    public class CommandSender
    {
        public IBus Bus { get; set; }

        public void Start()
        {
            Console.WriteLine("Press 'C' to send a command");
            Console.WriteLine("Press 'R' to send a request");
            Console.WriteLine("Press 'S' to start the saga");
            Console.WriteLine("Press 'E' to send a message that is marked as Express");
            Console.WriteLine("Press 'D' to send a large message that is marked to be sent using Data Bus");
            Console.WriteLine("Press 'X' to send a message that is marked with expiration time.");
            Console.WriteLine("To exit, press Ctrl + C");

            while (true)
            {
                var cmd = Console.ReadKey().Key.ToString().ToLower();
                switch (cmd)
                {
                    case "c":
                        SendCommand();
                        break;

                    case "r":
                        SendRequest();
                        break;

                    case "s":
                        StartSaga();
                        break;

                    case "e":
                        Express();
                        break;

                    case "d":
                        Data();
                        break;

                    case "x":
                        Expiration();
                        break;
                }
            }
        }

        /// <summary>
        /// Shut down server before sending this message, after 30 seconds, the message will be moved to Transactional dead-letter messages queue.
        /// </summary>
        private void Expiration()
        {
            Bus.Send<MessageThatExpires>(m => m.RequestId = Guid.NewGuid());
            Console.WriteLine("message with expiration was sent");
        }

        private void Data()
        {
            var requestId = Guid.NewGuid();

            Bus.Send<LargeMessage>(m =>
                                         {
                                             m.RequestId = requestId;
                                             m.LargeDataBus = new byte[1024 * 1024 * 5];
                                         });

            Console.WriteLine("Request sent id: " + requestId);
        }

        private void Express()
        {
            var requestId = Guid.NewGuid();

            Bus.Send<RequestExpress>(m =>
                                         {
                                             m.RequestId = requestId;
                                         });

            Console.WriteLine("Request sent id: " + requestId);
        }

        void StartSaga(string tennant = "")
        {
            var message = new StartSagaMessage
                              {
                                  OrderId = Guid.NewGuid()
                              };
            if (!string.IsNullOrEmpty(tennant))
            {
                Bus.SetMessageHeader(message, "tennant", tennant);
            }

            Bus.Send(message);
            Console.WriteLine("{0} - {1}", DateTime.Now.ToLongTimeString(), "Saga start message sent");
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
                .Register<CommandStatus>(outcome => Console.WriteLine("Server returned status: " + outcome));

            Console.WriteLine("Command sent id: " + commandId);

        }
    }
}