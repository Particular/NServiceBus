using NServiceBus.Hosting.Profiles;

namespace NServiceBus.Hosting.Azure.Profiles.Handlers
{
    internal class WithAppFabricQueuesProfileHandler : IHandleProfile<WithAppFabricQueues>
    {
        void IHandleProfile.ProfileActivated()
        {
            Configure.Instance
                .AppFabricQueue();

        }
    }
}