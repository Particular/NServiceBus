using System;
using NServiceBus;
using Messages;
using NServiceBus.Host;

namespace Client
{
    class ClientEndpoint : IMessageEndpoint
    {
        public IBus Bus { get; set; }

        public void OnStart()
        {
            Console.WriteLine("Press 'Enter' to send a message.To exit, Ctrl + C");

            Action handleInput = () =>
            {
                while (Console.ReadLine() != null)
                {
                    var m = new RequestDataMessage
                                {
                                    DataId = Guid.NewGuid(),
                                    String = "<node>it's my \"node\" & i like it<node>",
                                    SecretQuestion = "What's your favorite color?"
                                };

                    Console.WriteLine("Requesting to get data by id: {0}", m.DataId.ToString("N"));

                    Bus.OutgoingHeaders["Test"] = m.DataId.ToString("N");

                    //notice that we're passing the message as our state object - gives context for handling responses (especially if they arrive out of order).
                    Bus.Send(m).Register(RequestDataComplete, m);
                }
            };

            handleInput.BeginInvoke(null, null);
        }

        private void RequestDataComplete(IAsyncResult asyncResult)
        {
            Console.Out.WriteLine("Response with header 'Test' = {0}, 1 = {1}, 2 = {2}.", Bus.CurrentMessageContext.Headers["Test"], 
                Bus.CurrentMessageContext.Headers["1"], 
                Bus.CurrentMessageContext.Headers["2"]);

            var result = asyncResult.AsyncState as CompletionResult;

            if (result == null)
                return;
            if (result.Messages == null)
                return;
            if (result.Messages.Length == 0)
                return;
            if (result.State == null)
                return;

            var response = result.Messages[0] as DataResponseMessage;
            if (response == null)
                return;

            Console.WriteLine("Response received with description: {0}\nSecret answer: {1}", response.String, response.SecretAnswer.Value);
        }


        public void OnStop()
        {
        }
    }
}
