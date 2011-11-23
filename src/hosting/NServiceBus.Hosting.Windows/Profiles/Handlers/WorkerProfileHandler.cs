namespace NServiceBus.Hosting.Windows.Profiles.Handlers
{
    using System;
    using Hosting.Profiles;

    class WorkerProfileHandler : IHandleProfile<Worker>
    {
        public void ProfileActivated()
        {
            if (!RoutingConfig.IsConfiguredAsMasterNode)
                throw new InvalidOperationException("Running in the worker profile requires that a msternode has been configured, please add a MasterNodeLocatorConfig section");

            Configure.Instance.UseDistributor();
        }
    }
}