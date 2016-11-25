namespace NServiceBus.AcceptanceTests.Distributor
{
    using System.Threading.Tasks;
    using Features;
    using Transport;

    class FakeReadyMessageProcessor : Feature
    {
        protected override void Setup(FeatureConfigurationContext context)
        {
            context.AddSatelliteReceiver(
                "ReadyMessages",
                "ReadyMessages",
                TransportTransactionMode.TransactionScope,
                PushRuntimeSettings.Default,
                (config, errorContext) => RecoverabilityAction.ImmediateRetry(),
                (builder, messageContext) => Task.CompletedTask);
        }
    }
}