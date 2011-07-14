namespace NServiceBus
{
    /// <summary>
    /// Profile indicating that you want the host to automatically check if MSMQ is installed,
    /// install MSMQ if it isn't, check that the right components of MSMQ are active,
    /// change the active MSMQ components as needed, check that the MSMQ service is running,
    /// and run the MSMQ service if it isn't.
    /// </summary>
    public class InstallMsmq : IProfile
    {
    }
}
