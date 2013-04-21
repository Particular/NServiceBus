namespace NServiceBus
{
    /// <summary>
    /// Indicates that the infrastructure should configure to run on top of sql azure
    /// </summary>
    [ObsoleteEx(Replacement = "UsingTransport<WindowsAzureServiceBus> or UseTransport<WindowsAzureServiceBus>", TreatAsErrorFromVersion = "4.0", RemoveInVersion = "5.0")]
    public class OnSqlAzure : IProfile
    {
    }
}