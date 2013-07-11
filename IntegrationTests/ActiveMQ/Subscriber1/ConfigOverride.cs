namespace Subscriber1
{
    using NServiceBus.Config;
    using NServiceBus.Config.ConfigurationSource;

    //demonstrate how to override specific configuration sections
    class ConfigOverride : IProvideConfiguration<MessageForwardingInCaseOfFaultConfig>
    {
        public MessageForwardingInCaseOfFaultConfig GetConfiguration()
        {
            return new MessageForwardingInCaseOfFaultConfig
                       {
                           ErrorQueue = "error"
                       };
        }
    }
}