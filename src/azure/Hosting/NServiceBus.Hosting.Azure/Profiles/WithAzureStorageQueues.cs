namespace NServiceBus
{
    [ObsoleteEx(Replacement = "UsingTransport<WindowsAzureStorage> or UseTransport<WindowsAzureStorage>", TreatAsErrorFromVersion = "4.0", RemoveInVersion = "5.0")]
    public class WithAzureStorageQueues : IProfile
    {
    }
}