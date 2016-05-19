namespace NServiceBus
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Features;
    using Logging;

    class UnicastRouterTableUpdater : FeatureStartupTask
    {
        public UnicastRouterTableUpdater(IAsyncTimer timer, IList<UnicastRouter> routers, TimeSpan checkInterval)
        {
            this.timer = timer;
            this.routers = routers;
            this.checkInterval = checkInterval;
        }

        protected override Task OnStart(IMessageSession session)
        {
            timer.Start(ReloadData, checkInterval, ex => log.Error("Error while updating routing table", ex));
            return TaskEx.CompletedTask;
        }

        async Task ReloadData()
        {
            foreach (var router in routers)
            {
                await router.RebuildRoutingTable().ConfigureAwait(false);
            }
        }

        protected override Task OnStop(IMessageSession session)
        {
            return timer.Stop();
        }

        static readonly ILog log = LogManager.GetLogger(typeof(UnicastRouterTableUpdater));
        IAsyncTimer timer;
        IList<UnicastRouter> routers;
        TimeSpan checkInterval;
    }
}