﻿namespace NServiceBus.Features
{
    using System;

    /// <summary>
    /// Base for feature startup tasks.
    /// </summary>
    public abstract class FeatureStartupTask
    {
        /// <summary>
        /// Will be called when the endpoint starts up if the feature has been activated.
        /// </summary>
        [ObsoleteEx(TreatAsErrorFromVersion = "6", RemoveInVersion = "7", ReplacementTypeOrMember = "OnStart(IBusContext context)")]
        protected virtual void OnStart()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        ///  Will be called after an endpoint has stopped processing messages, if the feature has been activated.
        /// </summary>
        [ObsoleteEx(TreatAsErrorFromVersion = "6", RemoveInVersion = "7", ReplacementTypeOrMember = "OnStop(IBusContext context)")]
        protected virtual void OnStop()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Will be called after an endpoint has been started but before processing any messages, if the feature has been activated.
        /// </summary>
        /// <param name="context">Bus context.</param>
        protected abstract void OnStart(IBusContext context);

        /// <summary>
        /// Will be called after an endpoint has been started but before processing any messages, if the feature has been activated.
        /// </summary>
        /// <param name="context">Bus context.</param>
        protected virtual void OnStop(IBusContext context)
        {
        }
        
        internal void PerformStartup(IBusContext context)
        {
            OnStart(context);
        }

        internal void PerformStop(IBusContext context)
        {
            OnStop(context);
        }
    }
}