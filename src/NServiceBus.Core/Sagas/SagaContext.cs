namespace NServiceBus.Sagas
{
    using System;
    using NServiceBus.Saga;

    /// <summary>
    /// Context class that holds the current saga beeing processed
    /// </summary>
    public class SagaContext
    {
        /// <summary>
        /// The saga
        /// </summary>
        [ThreadStatic]
        public static ISaga Current;
    }
}