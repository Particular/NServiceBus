namespace Runner
{
    using System;
    using System.Threading.Tasks;
    using NServiceBus;

    public class StartActionRunner : IWantToRunWhenBusStartsAndStops
    {
        Action<ISendOnlyBus> seedAction;

        public StartActionRunner(Action<ISendOnlyBus> seedAction)
        {
            this.seedAction = seedAction;
        }

        public Task StartAsync(ISendOnlyBus bus)
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