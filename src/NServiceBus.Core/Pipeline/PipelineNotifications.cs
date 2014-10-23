namespace NServiceBus.Pipeline
{
    using System;

    /// <summary>
    /// Pipeline notifications
    /// </summary>
    public class PipelineNotifications : IDisposable
    {
        /// <summary>
        ///   Notification when a message is dequeued.
        /// </summary>
        public IObservable<IObservable<StepStarted>> ReceiveStarted
        {
            get { return receiveStarted; }
        }

        void IDisposable.Dispose()
        {
            //Injected
        }

        internal void InvokeReceiveStarted(IObservable<StepStarted> pipe)
        {
            receiveStarted.OnNext(pipe);
        }

        Observable<IObservable<StepStarted>> receiveStarted = new Observable<IObservable<StepStarted>>();
    }
}