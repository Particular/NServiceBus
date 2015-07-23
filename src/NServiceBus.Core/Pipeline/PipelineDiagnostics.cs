namespace NServiceBus
{
    using System;
    using NServiceBus.Pipeline;

    /// <summary>
    /// Provides access to diagnostics data for the pipelines.
    /// </summary>
    public class PipelineDiagnostics
    {
        /// <summary>
        /// Initializes a new instance of <see cref="PipelineDiagnostics"/>.
        /// </summary>
        public PipelineDiagnostics()
        {
            StepsDiagnostics = new Observable<StepStarted>();
        }

        internal Observable<StepStarted> StepsDiagnostics { get; private set; }


        /// <summary>
        /// Access to diagnostics for the steps of the pipeline.
        /// </summary>
        public IObservable<StepStarted> Steps{ get { return StepsDiagnostics; }}
    }
}