namespace NServiceBus
{
    using System;
    using NServiceBus.Hosting;
    using NServiceBus.Settings;
    using NServiceBus.Support;
    using Pipeline;
    using Transports;
    using Unicast;


    class AuditBehavior : PhysicalMessageProcessingStageBehavior
    {
        public HostInformation HostInformation { get; set; }

        public IAuditMessages MessageAuditer { get; set; }

        public string AuditQueue { get; set; }

        public TimeSpan? TimeToBeReceivedOnForwardedMessages { get; set; }

        public ReadOnlySettings Settings { get; set; }

        public override void Invoke(Context context, Action next)
        {
            next();

            var sendOptions = new SendOptions(AuditQueue)
            {
                TimeToBeReceived = TimeToBeReceivedOnForwardedMessages
            };

            context.PhysicalMessage.RevertToOriginalBodyIfNeeded();

            var outgoingMessage = new OutgoingMessage(context.PhysicalMessage.Id, context.PhysicalMessage.Headers, context.PhysicalMessage.Body);
            
            //set audit related headers
            outgoingMessage.Headers[Headers.ProcessingStarted] = DateTimeExtensions.ToWireFormattedString(context.Get<DateTime>("IncomingMessage.ProcessingStarted"));
            context.PhysicalMessage.Headers[Headers.ProcessingEnded] = DateTimeExtensions.ToWireFormattedString(context.Get<DateTime>("IncomingMessage.ProcessingEnded"));

            outgoingMessage.Headers[Headers.HostId] = HostInformation.HostId.ToString("N");
            outgoingMessage.Headers[Headers.HostDisplayName] = HostInformation.DisplayName;
            outgoingMessage.Headers[Headers.ProcessingMachine] = RuntimeEnvironment.MachineName;
            outgoingMessage.Headers[Headers.ProcessingEndpoint] = Settings.EndpointName();


            MessageAuditer.Audit(sendOptions, outgoingMessage);
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