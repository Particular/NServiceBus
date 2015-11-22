namespace NServiceBus
{
    using System;
    using System.Threading.Tasks;
    using NServiceBus.Logging;
    using NServiceBus.ObjectBuilder;

    /// <summary>
    /// A holder for that exposes access to the action defined by <see cref="ConfigureCriticalErrorAction.DefineCriticalErrorAction(BusConfiguration,Func{string, Exception, Task})"/>.
    /// </summary>
    /// <returns>
    /// Call <see cref="Raise"/> to trigger the action.
    /// </returns>
    public class CriticalError
    {
        Func<string, Exception, Task> onCriticalErrorAction;
        IBuilder builder;
      
        /// <summary>
        /// Initializes a new instance of <see cref="CriticalError"/>.
        /// </summary>
        /// <param name="onCriticalErrorAction">The action to execute when a critical error is triggered.</param>
        /// <param name="builder">The <see cref="IBuilder"/> instance.</param>
        public CriticalError(Func<string, Exception, Task> onCriticalErrorAction, IBuilder builder)
        {
            Guard.AgainstNull(nameof(builder), builder);

            this.onCriticalErrorAction = onCriticalErrorAction ?? DefaultCriticalErrorHandling;
            this.builder = builder;
        }

        Task DefaultCriticalErrorHandling(string errorMessage, Exception exception)
        {
            var components = builder.Build<IConfigureComponents>();
            if (!components.HasComponent<IEndpoint>())
            {
                return TaskEx.Completed;
            }

            return builder.Build<IEndpoint>().Stop();
        }

        /// <summary>
        /// Trigger the action defined by <see cref="ConfigureCriticalErrorAction.DefineCriticalErrorAction(BusConfiguration,Func{string, Exception, Task})"/>.
        /// </summary>
        public virtual Task Raise(string errorMessage, Exception exception)
        {
            Guard.AgainstNullAndEmpty(nameof(errorMessage), errorMessage);
            Guard.AgainstNull(nameof(exception), exception);
            LogManager.GetLogger("NServiceBus").Fatal(errorMessage, exception);

            return onCriticalErrorAction(errorMessage, exception);
        }
    }
}