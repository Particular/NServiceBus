namespace NServiceBus
{
    using System;
    using NServiceBus.Hosting;
    using Pipeline;
    using Transports;
    using Unicast;


    class AuditBehavior : PhysicalMessageProcessingStageBehavior
    {
        public HostInformation HostInformation { get; set; }

        public IAuditMessages MessageAuditer { get; set; }

        public Address AuditQueue { get; set; }

        public TimeSpan? TimeToBeReceivedOnForwardedMessages { get; set; }

        public override void Invoke(Context context, Action next)
        {
            next();

            var sendOptions = new SendOptions(AuditQueue)
            {
                TimeToBeReceived = TimeToBeReceivedOnForwardedMessages
            };

            //set audit related headers
            context.PhysicalMessage.Headers[Headers.ProcessingStarted] = DateTimeExtensions.ToWireFormattedString(context.Get<DateTime>("IncomingMessage.ProcessingStarted"));
            context.PhysicalMessage.Headers[Headers.ProcessingEnded] = DateTimeExtensions.ToWireFormattedString(context.Get<DateTime>("IncomingMessage.ProcessingEnded"));

            context.PhysicalMessage.Headers[Headers.HostId] = HostInformation.HostId.ToString("N");
            context.PhysicalMessage.Headers[Headers.HostDisplayName] = HostInformation.DisplayName;

            MessageAuditer.Audit(sendOptions, context.PhysicalMessage);
        }

        public class Registration:RegisterStep
        {
            public Registration()
                : base(WellKnownStep.AuditProcessedMessage, typeof(AuditBehavior), "Send a copy of the successfully processed message to the configured audit queue")
            {
                InsertBeforeIfExists("ReceivePerformanceDiagnosticsBehavior");
                InsertAfterIfExists("FirstLevelRetries");
            }
        }
    }
}