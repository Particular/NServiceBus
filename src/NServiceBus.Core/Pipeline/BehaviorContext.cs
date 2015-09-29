﻿namespace NServiceBus.Pipeline
{
    using Extensibility;
    using ObjectBuilder;

    /// <summary>
    /// Base class for a pipeline behavior.
    /// </summary>
    public abstract class BehaviorContext : ContextBag
    {
        /// <summary>
        /// Create an instance of <see cref="BehaviorContext"/>.
        /// </summary>
        /// <param name="parentContext">The parent context.</param>
        protected BehaviorContext(ContextBag parentContext) : base(parentContext)
        {
          
        }

        /// <summary>
        /// The current <see cref="IBuilder"/>.
        /// </summary>
        public IBuilder Builder
        {
            get
            {
                var rawBuilder = Get<IBuilder>();
                return rawBuilder;
            }
        }

        internal bool handleCurrentMessageLaterWasCalled;
    }
}