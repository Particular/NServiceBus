namespace NServiceBus.Gateway
{
    using log4net;
    using Unicast.Queuing;

    public class GatewayService : IWantToRunAtStartup
    {
        public string DefaultDestinationAddress { get; set; }

        public GatewayService(MsmqInputDispatcher inputDispatcher, IChannel channel, ISendMessages messageSender)
        {
            this.inputDispatcher = inputDispatcher;
            this.channel = channel;
            this.messageSender = messageSender;
        }

        public void Run()
        {
            inputDispatcher.Start();

            //todo start all the channels (when we support multiple channels)
            channel.MessageReceived += MessageReceivedOnChannel;
            channel.Start();
        }

        void MessageReceivedOnChannel(object sender, MessageForwardingArgs e)
        {
            var messageToSend = e.Message;

            string routeTo = Headers.RouteTo.Replace(HeaderMapper.NServiceBus + Headers.HeaderName + ".", "");
            var destination = DefaultDestinationAddress;
           
            if (messageToSend.Headers.ContainsKey(routeTo))
                destination = messageToSend.Headers[routeTo];
           
            Logger.Info("Sending message to " + destination);

            messageSender.Send(messageToSend, destination);
        }

        public void Stop()
        {
            channel.Stop();
        }



        static readonly ILog Logger = LogManager.GetLogger("NServiceBus.Gateway");
        readonly IChannel channel;
        readonly ISendMessages messageSender;
        readonly MsmqInputDispatcher inputDispatcher;

    }
}