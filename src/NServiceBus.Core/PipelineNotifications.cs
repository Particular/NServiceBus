namespace NServiceBus
{
    using System;
    using NServiceBus.Pipeline;

    /// <summary>
    /// </summary>
    public class PipelineNotifications : IDisposable
    {
        /// <summary>
        ///     Running instances.
        /// </summary>
        public IObservable<StepStarted> StepStarted
        {
            get { return stepStarted; }
        }

        /// <summary>
        ///     Step ended
        /// </summary>
        public IObservable<StepEnded> StepEnded
        {
            get { return stepEnded; }
        }

        /// <summary>
        ///     Running instances.
        /// </summary>
        public IObservable<PipeStarted> PipeStarted
        {
            get { return pipeStarted; }
        }

        /// <summary>
        ///     Step ended
        /// </summary>
        public IObservable<PipeEnded> PipeEnded
        {
            get { return pipeEnded; }
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        /// <filterpriority>2</filterpriority>
        public void Dispose()
        {
            //Injected
        }

        internal void InvokeStepStarted(StepStarted step)
        {
            stepStarted.Publish(step);
        }

        internal void InvokeStepEnded(StepEnded step)
        {
            stepEnded.Publish(step);
        }

        internal void InvokePipeStarted(PipeStarted pipe)
        {
            pipeStarted.Publish(pipe);
        }

        internal void InvokePipeEnded(PipeEnded pipe)
        {
            pipeEnded.Publish(pipe);
        }

        Observable<PipeEnded> pipeEnded = new Observable<PipeEnded>();
        Observable<PipeStarted> pipeStarted = new Observable<PipeStarted>();
        Observable<StepEnded> stepEnded = new Observable<StepEnded>();
        Observable<StepStarted> stepStarted = new Observable<StepStarted>();
    }
}