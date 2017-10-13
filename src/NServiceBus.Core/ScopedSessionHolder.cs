namespace NServiceBus
{
    using System.Threading;
    using Logging;

    class ScopedSessionHolder
    {
        static ILog Log = LogManager.GetLogger(typeof(ScopedSessionHolder));

        public AsyncLocal<IMessageSessionScoped> Session = new AsyncLocal<IMessageSessionScoped>(LogSessionChanged);

        static void LogSessionChanged(AsyncLocalValueChangedArgs<IMessageSessionScoped> sessionChangedEvent)
        {
            Log.DebugFormat("Scoped session changed from {0} to {1}{2}.", sessionChangedEvent.PreviousValue, sessionChangedEvent.CurrentValue, sessionChangedEvent.ThreadContextChanged ? " because the thread context has changed" : "");
        }
    }
}