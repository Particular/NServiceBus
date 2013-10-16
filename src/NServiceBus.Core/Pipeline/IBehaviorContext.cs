namespace NServiceBus.Pipeline
{
    using System;
    using System.ComponentModel;

    /// <summary>
    /// This bad boy is passed down into the behavior chain
    /// </summary>
    // hide for now until we have confirmed the API
    [EditorBrowsable(EditorBrowsableState.Advanced)]
    public interface IBehaviorContext
    {
        T Get<T>();
        void Set<T>(T t);
        TransportMessage TransportMessage { get; }
        object[] Messages { get; set; }

        /// <summary>
        /// Enters a new "trace context" which is a logically indented context that models the behavior call stack
        /// </summary>
        IDisposable TraceContextFor<T>();

        /// <summary>
        /// Logs a trace message using the current nesting level
        /// </summary>
        void Trace(string message, params object[] objs);

        bool DoNotContinueDispatchingMessageToHandlers { get; set; }
        T Get<T>(string key);
        void Set<T>(string key, T t);
    }
}