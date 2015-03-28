namespace NServiceBus
{
    using System;
    using System.Threading;
    using NServiceBus.Logging;
    using NServiceBus.ObjectBuilder;

    /// <summary>
    /// A holder for that exposes access to the action defined by <see cref="ConfigureCriticalErrorAction.DefineCriticalErrorAction(BusConfiguration,Action{string,Exception})"/>.
    /// </summary>
    /// <returns>
    /// Call <see cref="Raise"/> to trigger the action.
    /// </returns>
    public class CriticalError
    {
        Action<string, Exception> onCriticalErrorAction;
        readonly IBuilder builder;
      
        /// <summary>
        /// Creates an instance of <see cref="CriticalError"/>
        /// </summary>
        /// <param name="onCriticalErrorAction">The action to execute when a critical error is triggered.</param>
        /// <param name="builder">The <see cref="IBuilder"/> instance.</param>
        public CriticalError(Action<string, Exception> onCriticalErrorAction, IBuilder builder)
        {
            Guard.AgainstNull(builder, "builder");

            this.onCriticalErrorAction = onCriticalErrorAction;
            this.builder = builder;
        }

        void DefaultCriticalErrorHandling()
        {
            var components = builder.Build<IConfigureComponents>();
            if (!components.HasComponent<IBus>())
            {
                return;
            }

            builder.Build<IStartableBus>()
                .Dispose();
        }

        /// <summary>
        /// Trigger the action defined by <see cref="ConfigureCriticalErrorAction.DefineCriticalErrorAction(BusConfiguration,Action{string,Exception})"/>.
        /// </summary>
        public virtual void Raise(string errorMessage, Exception exception)
        {
            Guard.AgainstNullAndEmpty(errorMessage, "errorMessage");
            Guard.AgainstNull(exception, "exception");
            LogManager.GetLogger("NServiceBus").Fatal(errorMessage, exception);

            if (onCriticalErrorAction == null)
            {
                ThreadPool.QueueUserWorkItem(state => DefaultCriticalErrorHandling());
            }
            else
            {
                ThreadPool.QueueUserWorkItem(state => onCriticalErrorAction(errorMessage, exception));
            }
        }
    }
}