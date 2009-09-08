using NServiceBus.Host.Profiles;

namespace NServiceBus.Host.Internal.ProfileHandlers
{
    /// <summary>
    /// Installs and starts MSMQ if necessary.
    /// </summary>
    public class InstallMsmqProfileHandler : IHandleProfile<InstallMsmq>
    {
        void IHandleProfile.Init(IConfigureThisEndpoint specifier)
        {
            Utils.MsmqInstallation.StartMsmqIfNecessary();
        }
    }
}
