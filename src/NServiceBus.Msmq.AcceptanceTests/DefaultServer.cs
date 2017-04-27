namespace NServiceBus.AcceptanceTests.EndpointTemplates
{
    using Features;

    public class DefaultServer : BasicServer
    {
        protected override void ApplyConfig(EndpointConfiguration configuration)
        {
            configuration.DisableFeature<TimeoutManager>();

            var recoverability = configuration.Recoverability();
            recoverability.Delayed(delayed => delayed.NumberOfRetries(0));
            recoverability.Immediate(immediate => immediate.NumberOfRetries(0));
            configuration.SendFailedMessagesTo("error");
        }
    }
}