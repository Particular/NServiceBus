namespace NServiceBus.Features
{
    using System;
    using System.Threading.Tasks;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Linq;

    /// <summary>
    /// Base for feature startup tasks.
    /// </summary>
    public abstract class FeatureStartupTask
    {
        /// <summary>
        /// No feature startup tasks.
        /// </summary>
        public static IReadOnlyCollection<FeatureStartupTask> None { get; } = new ReadOnlyCollection<FeatureStartupTask>(new List<FeatureStartupTask>());

        /// <summary>
        /// Some feature startup tasks.
        /// </summary>
        public static IReadOnlyCollection<FeatureStartupTask> Some(params FeatureStartupTask[] tasks)
        {
            return new ReadOnlyCollection<FeatureStartupTask>(tasks.ToList());
        } 

        /// <summary>
        /// Will be called when the endpoint starts up if the feature has been activated.
        /// </summary>
        [ObsoleteEx(
            TreatAsErrorFromVersion = "6", 
            RemoveInVersion = "7",
            ReplacementTypeOrMember = "OnStart(IBusContext context)")]
        protected virtual void OnStart()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        ///  Will be called after an endpoint has stopped processing messages, if the feature has been activated.
        /// </summary>
        [ObsoleteEx(
            TreatAsErrorFromVersion = "6", 
            RemoveInVersion = "7", 
            ReplacementTypeOrMember = "OnStop(IBusContext context)")]
        protected virtual void OnStop()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Will be called after an endpoint has been started but before processing any messages, if the feature has been activated.
        /// </summary>
        /// <param name="context">Bus context.</param>
        protected abstract Task OnStart(IBusContext context);

        /// <summary>
        /// Will be called after an endpoint has been started but before processing any messages, if the feature has been activated.
        /// </summary>
        /// <param name="context">Bus context.</param>
        protected virtual Task OnStop(IBusContext context)
        {
            return TaskEx.Completed;
        }
        
        internal Task PerformStartup(IBusContext context)
        {
            return OnStart(context);
        }

        internal Task PerformStop(IBusContext context)
        {
            return OnStop(context);
        }
    }
}