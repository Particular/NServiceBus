namespace NServiceBus.Routing
{
    using System;
    using System.Net;
    using System.Threading;
    using Janitor;
    using NServiceBus.Logging;

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

        public void StartTimer(string callbackUrl)
        {
            lock (lockObj)
            {
                if (timer != null)
                {
                    timer.Dispose();
                }
                timer = new Timer(NotifyParties, callbackUrl, idleTimeToWaitForBeforeNotifying, Timeout.InfiniteTimeSpan);
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

        void NotifyParties(object url)
        {
            notifications.Endpoint.InvokeSafeToDisconnect(idleTimeToWaitForBeforeNotifying);
            
            if (url != null)
            {
                CallCallbackUrl(url.ToString());
            }
        }

        static void CallCallbackUrl(string url)
        {
            try
            {
                var request = WebRequest.CreateHttp(url);
                request.ContentType = "application/x-www-form-urlencoded";
                request.Method = "POST";
                request.UserAgent = "NServiceBus-CallbackHome";
                request.Timeout = TimeSpan.FromSeconds(5).Milliseconds;
                request.GetResponse();
            }
            catch (Exception ex)
            {
                logger.Error(string.Format("Failed to notify callback on '{0}'.", url), ex);
            }
        }

        static ILog logger = LogManager.GetLogger<NoMessageBacklogNotifier>();
        readonly BusNotifications notifications;
        TimeSpan idleTimeToWaitForBeforeNotifying;
        object lockObj = new object();
        Timer timer;
    }
}