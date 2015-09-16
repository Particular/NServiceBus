namespace NServiceBus.Timeout.Hosting.Windows
{
    using System;
    using System.Text;
    using Core;
    using NServiceBus.Logging;
    using Satellites;
    using Transports;
    using Unicast.Transport;

    class TimeoutDispatcherProcessor : IAdvancedSatellite
    {
        static ILog logger = LogManager.GetLogger<TimeoutDispatcherProcessor>();

        public TimeoutDispatcherProcessor()
        {
            Disabled = true;
        }

        public ISendMessages MessageSender { get; set; }

        public IPersistTimeouts TimeoutsPersister { get; set; }

        public TimeoutPersisterReceiver TimeoutPersisterReceiver { get; set; }

        public Configure Configure { get; set; }
      
        public Address InputAddress { get; set; }

        public bool Disabled { get; set; }

        public bool Handle(TransportMessage message)
        {
            var timeoutId = message.Headers["Timeout.Id"];
            TimeoutData timeoutData;

            if (TimeoutsPersister.TryRemove(timeoutId, out timeoutData))
            {
                try
                {
                    MessageSender.Send(timeoutData.ToTransportMessage(), timeoutData.ToSendOptions(Configure.LocalAddress));
                }
                catch (Exception ex)
                {
                    logger.Warn("Failed to dispatch the timeout message " + TimeoutDataToString(timeoutData), ex);
                    timeoutData.Id = null;
                    timeoutData.Time = DateTime.UtcNow.AddSeconds(5);
                    try
                    {
                        TimeoutsPersister.Add(timeoutData);
                    }
                    catch (Exception e)
                    {
                        logger.Error("Failed to dispatch the timeout message. Failed add the timeout message to storage again. " + TimeoutDataToString(timeoutData) + ".", e);
                    }
                }
            }

            return true;
        }

        string TimeoutDataToString(TimeoutData timeout)
        {
            var sb = new StringBuilder();
            sb.AppendLine(timeout.ToString());
            sb.AppendLine("Headers: ");
            foreach (var header in timeout.Headers)
                sb.AppendLine(header.Key + " : " + header.Value);
            string contentType;
            timeout.Headers.TryGetValue(Headers.ContentType, out contentType);
            if (contentType != null && contentType.Contains("binary") == false)
            {
                sb.AppendLine("Body: ");
                sb.AppendLine(Encoding.UTF8.GetString(timeout.State));
            }
            return sb.ToString();
        }

        public void Start()
        {
            TimeoutPersisterReceiver.Start();
        }

        public void Stop()
        {
            TimeoutPersisterReceiver.Stop();
        }

        public Action<TransportReceiver> GetReceiverCustomization()
        {
            return receiver =>
            {
                //TODO: The line below needs to change when we refactor the slr to be:
                // transport.DisableSLR() or similar
                receiver.FailureManager = new ManageMessageFailuresWithoutSlr(receiver.FailureManager, MessageSender, Configure);
            };
        }
    }
}
