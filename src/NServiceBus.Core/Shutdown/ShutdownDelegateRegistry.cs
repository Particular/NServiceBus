namespace NServiceBus
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Runtime.CompilerServices;
    using System.Threading.Tasks;
    using Logging;
    using Shutdown;

    class ShutdownDelegateRegistry : IRegisterShutdownDelegates, IContainShutdownDelegates
    {
        public void Register(Action action, [CallerMemberName]string caller = null)
        {
            AddToDelegateDictionary(() =>
            {
                action();
                return completed;
            }, caller);
        }

        public void Register(Func<Task> func, [CallerMemberName]string caller = null)
        {
            AddToDelegateDictionary(func, caller);
        }

        void AddToDelegateDictionary(Func<Task> func, string caller)
        {
            shutdownDelegates.Enqueue(new CallerDelegateAssociation() { Caller = caller, Delegate = func });
        }

        public async Task Execute()
        {
            if (shutdownDelegates.Count == 0)
            {
                return;
            }

            var shutdownDelegateTasks = new List<Task>();
            foreach (var association in shutdownDelegates)
            {
                var callingMethod = association.Caller;
                var shutdownDelegate = association.Delegate;

                try
                {
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

        class CallerDelegateAssociation
        {
            public string Caller { get; set; }
            public Func<Task> Delegate { get; set; }
        }

        Task completed = Task.FromResult(0);
        ConcurrentQueue<CallerDelegateAssociation> shutdownDelegates = new ConcurrentQueue<CallerDelegateAssociation>();
        static ILog Log = LogManager.GetLogger<ShutdownDelegateRegistry>();
    }
}