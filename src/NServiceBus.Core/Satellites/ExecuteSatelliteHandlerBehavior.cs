namespace NServiceBus
{
    using System;
    using NServiceBus.Pipeline;
    using NServiceBus.Satellites;

    class ExecuteSatelliteHandlerBehavior: PhysicalMessageProcessingStageBehavior
    {
        public override void Invoke(Context context, Action next)
        {
            var satellite = context.Get<ISatellite>();

            context.Set("TransportReceiver.MessageHandledSuccessfully", satellite.Handle(context.PhysicalMessage));
        }

        public class Registration : RegisterStep
        {
            public Registration()
                : base("SatelliteHandlerExecutor", typeof(ExecuteSatelliteHandlerBehavior), "Invokes the decryption logic")
            {
                InsertBeforeIfExists("FirstLevelRetries");
                InsertBeforeIfExists("ReceivePerformanceDiagnosticsBehavior");
            }
        }
    }
}