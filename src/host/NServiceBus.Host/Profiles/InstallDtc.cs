namespace NServiceBus.Host.Profiles
{
    /// <summary>
    /// Profile indicating that you want the host to automatically check if the Distributed Transaction Coordinator
    /// windows service has its security settings configured correctly, and if they aren't, set the correct settings,
    /// check that the service is running, and if it isn't, run the MSDTC service.
    /// </summary>
    public class InstallDtc : IProfile
    {
    }
}
