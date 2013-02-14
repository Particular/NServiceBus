namespace NServiceBus
{
    [ObsoleteEx(Replacement = "UsingTransport<WindowsAzureServiceBus> or UseTransport<WindowsAzureServiceBus>", TreatAsErrorFromVersion = "4.0", RemoveInVersion = "5.0")]
    public class WithAzureServiceBusQueues : IProfile
    {
    }
}