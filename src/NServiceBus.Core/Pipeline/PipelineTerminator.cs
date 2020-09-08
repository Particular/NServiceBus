namespace NServiceBus.Pipeline
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Marks the inner most behavior of the pipeline.
    /// </summary>
    /// <typeparam name="T">The pipeline context type to terminate.</typeparam>
    public abstract class PipelineTerminator<T> : StageConnector<T, PipelineTerminator<T>.ITerminatingContext>, IPipelineTerminator where T : IBehaviorContext
    {
        /// <summary>
        /// This method will be the final one to be called before the pipeline starts to traverse back up the "stack".
        /// </summary>
        /// <param name="context">The current context.</param>
        /// <param name="cancellationToken"></param>
        protected abstract Task Terminate(T context, CancellationToken cancellationToken);

        /// <summary>
        /// Invokes the terminate method.
        /// </summary>
        /// <param name="context">Context object.</param>
        /// <param name="next">Ignored since there by definition is no next behavior to call.</param>
        /// <param name="cancellationToken"></param>
        public sealed override Task Invoke(T context, Func<ITerminatingContext, CancellationToken, Task> next, CancellationToken cancellationToken)
        {
            Guard.AgainstNull(nameof(next), next);

            return Terminate(context, cancellationToken);
        }

        /// <summary>
        /// A well-known context that terminates the pipeline.
        /// </summary>
        public interface ITerminatingContext : IBehaviorContext
        {
        }
    }
}