namespace NServiceBus.Features
{
    using System.Threading.Tasks;

    class TimeoutPollerRunner : FeatureStartupTask
    {
        ExpiredTimeoutsPoller poller;

        public TimeoutPollerRunner(ExpiredTimeoutsPoller poller)
        {
            this.poller = poller;
        }

        protected override Task OnStart(IBusSession session)
        {
            poller.Start();
            return TaskEx.Completed;
        }

        protected override Task OnStop(IBusSession session)
        {
            return poller.Stop();
        }
    }
}