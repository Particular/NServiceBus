using System.Threading;

namespace NServiceBus.Features
{
    using System.Threading.Tasks;

    class TimeoutPollerRunner : FeatureStartupTask
    {
        public TimeoutPollerRunner(ExpiredTimeoutsPoller poller)
        {
            this.poller = poller;
        }

        protected override Task OnStart(IMessageSession session, CancellationToken cancellationToken = default)
        {
            poller.Start(cancellationToken);
            return Task.CompletedTask;
        }

        protected override Task OnStop(IMessageSession session, CancellationToken cancellationToken = default)
        {
            return poller.Stop();
        }

        ExpiredTimeoutsPoller poller;
    }
}