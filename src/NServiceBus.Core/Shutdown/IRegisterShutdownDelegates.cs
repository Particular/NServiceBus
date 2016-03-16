namespace NServiceBus.Shutdown
{
    using System;
    using System.Threading.Tasks;

    /// <summary>
    /// Allow registering actions to call when endpoint stops.
    /// </summary>
    public interface IRegisterShutdownDelegates
    {
        /// <summary>
        /// Register an action.
        /// </summary>
        void Register(Action action, string caller = null);

        /// <summary>
        /// Register an async delegate.
        /// </summary>
        void Register(Func<Task> func, string caller = null);
    }
}