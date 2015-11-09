namespace Runner
{
    using System;
    using System.Threading.Tasks;
    using NServiceBus;

    public class StartActionRunner : IWantToRunWhenBusStartsAndStops
    {
        Action<IBusContext> seedAction;

        public StartActionRunner(Action<IBusContext> seedAction)
        {
            this.seedAction = seedAction;
        }

        public Task StartAsync(IBusContext context)
        {
            seedAction(context);
            return Task.FromResult(0);
        }

        public Task StopAsync(IBusContext context)
        {
            return Task.FromResult(0);
        }
    }
}