#pragma warning disable 1591
// ReSharper disable UnusedParameter.Global
namespace NServiceBus
{
    using System;

    [ObsoleteEx(Message = "Please use 'UsingTransport<Msmq>' on your 'IConfigureThisEndpoint' class or use 'Configure.With(b => b.UseTransport<Msmq>())' as part of the the fluent API.", RemoveInVersion = "6.0", TreatAsErrorFromVersion = "5.0")]
    public static class ConfigureMsmqMessageQueue
    {
        [ObsoleteEx(Message = "Please use 'UsingTransport<Msmq>' on your 'IConfigureThisEndpoint' class or use 'Configure.With(b => b.UseTransport<Msmq>())' as part of the the fluent API.", RemoveInVersion = "6.0", TreatAsErrorFromVersion = "5.0")]
        public static Configure MsmqTransport(this Configure config)
        {
            throw new NotImplementedException();
        }
    }
}