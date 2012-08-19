namespace NServiceBus.Timeout.Hosting.Windows
{
    using System;
    using Core;
    using Core.Dispatch;
    using ObjectBuilder;
    using Satellites;
    using Unicast.Queuing;
    using System.Configuration;
    using Logging;

    public class TimeoutMessageProcessor : ISatellite
    {
        ILog Logger = LogManager.GetLogger("TimeoutMessageProcessor");
        public IBuilder Builder { get; set; }
        public static Func<IReceiveMessages> MessageReceiverFactory { get; set; }
        public Address InputAddress { get; set; }
        public bool Disabled { get; set; }
        public ISendMessages MessageSender { get; set; }       

        public void Handle(TransportMessage message)
        {
            if (Disabled)
            {
                throw new ConfigurationErrorsException("The TimeoutManager satellite is invoked, but disabled.");
            }

            //dispatch request will arrive at the same input so we need to make sure to call the correct handler
            if (message.Headers.ContainsKey(TimeoutDispatcher.TimeoutIdToDispatchHeader))
                Builder.Build<TimeoutDispatchHandler>().Handle(message);
            else
                Builder.Build<TimeoutTransportMessageHandler>().Handle(message);
        }

        public void Start(){}
        public void Stop(){}
    }
}