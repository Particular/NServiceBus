namespace NServiceBus.Unicast
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using NServiceBus.Logging;

    class StartAndStoppablesRunner
    {
        public StartAndStoppablesRunner(IEnumerable<IWantToRunWhenBusStartsAndStops> wantToRunWhenBusStartsAndStops)
        {
            wantToRunWhenBusStartsAndStopses = wantToRunWhenBusStartsAndStops;
        }

        public Task StartAsync()
        {
            var startableTasks = new List<Task>();
            foreach (var startable in wantToRunWhenBusStartsAndStopses)
            {
                var task = startable.StartAsync();

                var startable1 = startable;
                task.ContinueWith(t =>
                {
                    thingsRanAtStartup.Add(startable1);
                    Log.DebugFormat("Started {0}.", startable1.GetType().AssemblyQualifiedName);
                }, TaskContinuationOptions.OnlyOnRanToCompletion | TaskContinuationOptions.ExecuteSynchronously);
                task.ContinueWith(t =>
                {
                    Log.Error($"Startup task {startable1.GetType().AssemblyQualifiedName} failed to complete.", t.Exception);
                }, TaskContinuationOptions.OnlyOnFaulted | TaskContinuationOptions.ExecuteSynchronously);

                startableTasks.Add(task);
            }

            return Task.WhenAll(startableTasks.ToArray());
        }

        public async Task StopAsync()
        {
            var stoppables = Interlocked.Exchange(ref thingsRanAtStartup, new ConcurrentBag<IWantToRunWhenBusStartsAndStops>());
            if (!stoppables.Any())
            {
                return;
            }

            var stoppableTasks = new List<Task>();
            foreach (var stoppable in stoppables)
            {
                try
                {
                    var task = stoppable.StopAsync();

                    var stoppable1 = stoppable;
                    task.ContinueWith(t =>
                    {
                        thingsRanAtStartup.Add(stoppable1);
                        Log.DebugFormat("Stopped {0}.", stoppable1.GetType().AssemblyQualifiedName);
                    }, TaskContinuationOptions.OnlyOnRanToCompletion | TaskContinuationOptions.ExecuteSynchronously)
                    .Ignore();
                    task.ContinueWith(t =>
                    {
                        Log.Fatal($"Startup task {stoppable1.GetType().AssemblyQualifiedName} failed to stop.", t.Exception);
                        t.Exception.Flatten().Handle(e => true);
                    }, TaskContinuationOptions.OnlyOnFaulted | TaskContinuationOptions.ExecuteSynchronously)
                    .Ignore();

                    stoppableTasks.Add(task);
                }
                catch (Exception e)
                {
                    Log.Fatal("Startup task failed to stop.", e);
                }
            }

            try
            {
                await Task.WhenAll(stoppableTasks.ToArray());
            }
            catch
            {
                // ignore because we want to shutdown no matter what.
            }
        }

        readonly IEnumerable<IWantToRunWhenBusStartsAndStops> wantToRunWhenBusStartsAndStopses;
        ConcurrentBag<IWantToRunWhenBusStartsAndStops> thingsRanAtStartup = new ConcurrentBag<IWantToRunWhenBusStartsAndStops>();
        static ILog Log = LogManager.GetLogger<StartAndStoppablesRunner>();
    }
}