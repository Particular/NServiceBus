namespace NServiceBus
{
    /// <summary>
    /// Indicates that the infrastructure should configure to run on top of azure table storage
    /// </summary>
    [ObsoleteEx(Replacement = "UsingTransport<WindowsAzureServiceBus> or UseTransport<WindowsAzureServiceBus>", TreatAsErrorFromVersion = "4.0", RemoveInVersion = "5.0")]
    public class OnAzureTableStorage : IProfile
    {
    }
}