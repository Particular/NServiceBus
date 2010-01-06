using NServiceBus.Hosting.Profiles;

namespace NServiceBus.Host.Profiles.Handlers
{
    /// <summary>
    /// Installs the distributed transaction coordinator.
    /// </summary>
    public class InstallDtcProfileHandler : IHandleProfile<InstallDtc>
    {
        void IHandleProfile.ProfileActivated()
        {
            Utils.DtcUtil.StartDtcIfNecessary();
        }
    }
}