namespace NServiceBus
{
    [ObsoleteEx(Replacement = "UsingTransport<AzureStorageQueue> or UseTransport<AzureStorageQueue>", TreatAsErrorFromVersion = "4.0", RemoveInVersion = "5.0")]
    public class WithAzureStorageQueues : IProfile
    {
    }
}