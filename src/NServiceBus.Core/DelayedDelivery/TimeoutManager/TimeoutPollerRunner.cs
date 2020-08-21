namespace NServiceBus.Features
{
    using System.Threading.Tasks;

    class TimeoutPollerRunner : FeatureStartupTask
    {
        public TimeoutPollerRunner(ExpiredTimeoutsPoller poller)
        {
            this.poller = poller;
        }

        protected override Task OnStart(IMessageSession session)
        {
            poller.Start();
            return Task.CompletedTask;
        }

        protected override Task OnStop(IMessageSession session)
        {
            return poller.Stop();
        }

        ExpiredTimeoutsPoller poller;
    }
}