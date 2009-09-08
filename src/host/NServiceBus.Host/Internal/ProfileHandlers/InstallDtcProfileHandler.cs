using NServiceBus.Host.Profiles;

namespace NServiceBus.Host.Internal.ProfileHandlers
{
    /// <summary>
    /// Installs the distributed transaction coordinator.
    /// </summary>
    public class InstallDtcProfileHandler : IHandleProfile<InstallDtc>
    {
        void IHandleProfile.Init(IConfigureThisEndpoint specifier)
        {
            Utils.DtcUtil.StartDtcIfNecessary();
        }
    }
}
