namespace NServiceBus.Hosting.Windows.Profiles.Handlers
{
    using System;
    using Config;
    using Hosting.Profiles;

    class WorkerProfileHandler : IHandleProfile<Worker>
    {
        public void ProfileActivated()
        {
            if (Configure.GetConfigSection<MasterNodeLocatorConfig>() == null)
                throw new InvalidOperationException("Running in the worker profile requires that a masternode has been configured, please add a MasterNodeLocatorConfig section");

            Configure.Instance.UseDistributor();
        }
    }
}