namespace Runner
{
    using NServiceBus.Config;
    using NServiceBus.Config.ConfigurationSource;

    class MessageForwardingInCaseOfFaultConfigOverride : IProvideConfiguration<MessageForwardingInCaseOfFaultConfig>
    {
        public MessageForwardingInCaseOfFaultConfig GetConfiguration()
        {
            return new MessageForwardingInCaseOfFaultConfig()
            {
                ErrorQueue = "error"
            };
        }
    }
}