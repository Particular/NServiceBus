namespace Runner
{
    using System;
    using System.Threading.Tasks;
    using NServiceBus;

    public class StartActionRunner : IWantToRunWhenBusStartsAndStops
    {
        Action<IBusInterface> seedAction;

        public StartActionRunner(Action<IBusInterface> seedAction)
        {
            this.seedAction = seedAction;
        }

        public Task StartAsync(IBusInterface bus)
        {
            seedAction(bus);
            return Task.FromResult(0);
        }

        public Task StopAsync()
        {
            return Task.FromResult(0);
        }
    }
}