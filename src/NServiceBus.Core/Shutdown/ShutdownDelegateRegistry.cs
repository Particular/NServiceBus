namespace NServiceBus
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Threading.Tasks;
    using Logging;
    using Shutdown;

    class ShutdownDelegateRegistry : IRegisterShutdownDelegates, IContainShutdownDelegates
    {
        public void Register(Action action)
        {
            AddToDelegateDictionary(() =>
            {
                action();
                return completed;
            });
        }

        public void Register(Func<Task> func)
        {
            AddToDelegateDictionary(func);
        }

        void AddToDelegateDictionary(Func<Task> func)
        {
            var frame = new StackFrame(1);
            var method = frame.GetMethod();
            var type = method.DeclaringType;
            var methodName = method.Name;

            shutdownDelegates.TryAdd($"{type}.{methodName}", func);
        }

        public async Task Execute()
        {
            if (shutdownDelegates.Count == 0)
            {
                return;
            }

            var shutdownDelegateTasks = new List<Task>();
            foreach (var callingMethod in shutdownDelegates.Keys)
            {
                try
                {
                    var shutdownDelegate = shutdownDelegates[callingMethod];
                    var task = shutdownDelegate.Invoke();

                    task.ContinueWith(t =>
                    {
                        Log.DebugFormat($"Executed shutdown delegate for {callingMethod}.");
                    }, TaskContinuationOptions.OnlyOnRanToCompletion | TaskContinuationOptions.ExecuteSynchronously).Ignore();

                    task.ContinueWith(t =>
                    {
                        Log.Fatal($"Shutdown delegate for {callingMethod} failed to execute.", t.Exception);
                        t.Exception.Flatten().Handle(e => true);
                    }, TaskContinuationOptions.OnlyOnFaulted | TaskContinuationOptions.ExecuteSynchronously).Ignore();

                    shutdownDelegateTasks.Add(task);
                }
                catch (Exception e)
                {
                    Log.Fatal($"Shutdown task failed for {callingMethod}.", e);
                }
            }

            await Task.WhenAll(shutdownDelegateTasks.ToArray()).ConfigureAwait(false);
        }

        Task completed = Task.FromResult(0);
        ConcurrentDictionary<string, Func<Task>> shutdownDelegates = new ConcurrentDictionary<string, Func<Task>>();
        static ILog Log = LogManager.GetLogger<ShutdownDelegateRegistry>();
    }
}