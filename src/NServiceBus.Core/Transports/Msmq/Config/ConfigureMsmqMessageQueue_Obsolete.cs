#pragma warning disable 1591
// ReSharper disable UnusedParameter.Global
namespace NServiceBus
{
    [ObsoleteEx(Message = "Please use UsingTransport<Msmq> on your IConfigureThisEndpoint class or use .UseTransport<Msmq>() as part of the the fluent API.", RemoveInVersion = "6.0", TreatAsErrorFromVersion = "5.0")]
    public static class ConfigureMsmqMessageQueue
    {
        [ObsoleteEx(Message = "Please use UsingTransport<Msmq> on your IConfigureThisEndpoint class or use .UseTransport<Msmq>() as part of the the fluent API.", RemoveInVersion = "6.0", TreatAsErrorFromVersion = "5.0")]
        public static Configure MsmqTransport(this Configure config)
        {
            return config.UseTransport<Msmq>();
        }
    }
}