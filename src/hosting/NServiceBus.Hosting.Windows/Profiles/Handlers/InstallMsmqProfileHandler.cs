using NServiceBus.Hosting.Profiles;

namespace NServiceBus.Host.Profiles.Handlers
{
    /// <summary>
    /// Installs and starts MSMQ if necessary.
    /// </summary>
    public class InstallMsmqProfileHandler : IHandleProfile<InstallMsmq>
    {
        void IHandleProfile.ProfileActivated()
        {
            Utils.MsmqInstallation.StartMsmqIfNecessary();
        }
    }
}