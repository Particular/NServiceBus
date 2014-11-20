namespace NServiceBus
{
    using System;
    using System.Collections.Generic;
    using NServiceBus.Routing;

    /// <summary>
    ///     Endpoint notifications
    /// </summary>
    public class EndpointNotifications : IDisposable
    {
        /// <summary>
        ///     Notification when an endpoint has been idled for a configured amount of time
        ///     and it should be safe to shut it down without leaving a backlog of messages.
        /// </summary>
        public IObservable<SafeDisconnect> SafeToDisconnect
        {
            get { return disconnectEventList; }
        }

        internal void InvokeSafeToDisconnect(Dictionary<string, string> headers)
        {
            disconnectEventList.Publish(new SafeDisconnect(new Dictionary<string, string>(headers)));
        }

        void IDisposable.Dispose()
        {
            // Injected
        }

        Observable<SafeDisconnect> disconnectEventList = new Observable<SafeDisconnect>();
    }
}