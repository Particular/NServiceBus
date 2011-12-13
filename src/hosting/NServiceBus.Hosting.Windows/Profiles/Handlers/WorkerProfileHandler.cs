namespace NServiceBus.Hosting.Windows.Profiles.Handlers
{
    using Hosting.Profiles;

    class WorkerProfileHandler : IHandleProfile<Worker>
    {
        public void ProfileActivated()
        {
            Configure.Instance.EnlistWithDistributor();
        }
    }
}