namespace NServiceBus.Sagas
{
    using System;
    using Saga;

    /// <summary>
    /// Context class that holds the current saga being processed
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