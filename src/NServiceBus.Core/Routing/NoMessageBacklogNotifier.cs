namespace NServiceBus.Routing
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using Janitor;

    [SkipWeaving]
    class NoMessageBacklogNotifier : IDisposable
    {
        public NoMessageBacklogNotifier(BusNotifications notifications)
        {
            this.notifications = notifications;
            idleTimeToWaitForBeforeNotifying = TimeSpan.FromSeconds(30);
        }

        public void Dispose()
        {
            if (timer == null)
            {
                return;
            }

            using (var waitHandle = new ManualResetEvent(false))
            {
                timer.Dispose(waitHandle);

                waitHandle.WaitOne();
            }
        }

        public void StartTimer(Dictionary<string, string> headers)
        {
            lock (lockObj)
            {
                if (timer != null)
                {
                    timer.Dispose();
                }
                timer = new Timer(NotifyParties, headers, idleTimeToWaitForBeforeNotifying, Timeout.InfiniteTimeSpan);
            }
        }

        public void ResetTimer()
        {
            if (timer == null)
            {
                return;
            }

            lock (lockObj)
            {
                timer.Change(idleTimeToWaitForBeforeNotifying, Timeout.InfiniteTimeSpan);
            }
        }

        void NotifyParties(object headers)
        {
            notifications.Endpoint.InvokeSafeToDisconnect((Dictionary<string, string>) headers);
        }

        readonly BusNotifications notifications;
        TimeSpan idleTimeToWaitForBeforeNotifying;
        object lockObj = new object();
        Timer timer;
    }
}