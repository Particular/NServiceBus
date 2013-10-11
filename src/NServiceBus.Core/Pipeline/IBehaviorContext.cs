namespace NServiceBus.Pipeline
{
    using System;

    /// <summary>
    /// This bad boy is passed down into the behavior chain
    /// </summary>
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
    }
}