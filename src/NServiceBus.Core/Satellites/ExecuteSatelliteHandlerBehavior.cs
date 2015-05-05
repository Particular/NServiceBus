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

            if (!satellite.Handle(context.PhysicalMessage))
            {
                context.AbortReceiveOperation = true;
            }
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