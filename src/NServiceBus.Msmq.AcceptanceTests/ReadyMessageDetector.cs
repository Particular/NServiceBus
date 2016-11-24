namespace NServiceBus.AcceptanceTests.ScaleOut
{
    using System.Threading.Tasks;
    using Features;
    using ObjectBuilder;
    using Transport;

    public class ReadyMessageDetector : Feature
    {
        protected override void Setup(FeatureConfigurationContext context)
        {
            context.AddSatelliteReceiver("ReadyMessageDetector", context.Settings.EndpointName() + ".Distributor", TransportTransactionMode.ReceiveOnly, PushRuntimeSettings.Default,
                OnError, OnMessage);
        }

        static Task OnMessage(IBuilder builder, MessageContext message)
        {
            var context = builder.Build<DistributorContext>();
            string capacityString;
            string sessionId;
            if (!message.Headers.TryGetValue("NServiceBus.Distributor.WorkerSessionId", out sessionId))
            {
                return Task.CompletedTask;
            }
            context.WorkerSessionId = sessionId;
            if (message.Headers.TryGetValue("NServiceBus.Distributor.WorkerCapacityAvailable", out capacityString))
            {
                var cap = int.Parse(capacityString);
                if (cap == 1) //If it is not 1 then we got the initial ready message.
                {
                    context.ReceivedReadyMessage = true;
                }
            }
            return Task.CompletedTask;
        }

        static RecoverabilityAction OnError(RecoverabilityConfig arg1, ErrorContext arg2)
        {
            return RecoverabilityAction.ImmediateRetry();
        }
    }
}