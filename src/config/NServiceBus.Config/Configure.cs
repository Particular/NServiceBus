using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ObjectBuilder;

namespace NServiceBus
{
    /// <summary>
    /// Central configuration entry point for NServiceBus.
    /// </summary>
    public class Configure
    {
        /// <summary>
        /// Provides static access to object builder functionality.
        /// </summary>
        public static IBuilder ObjectBuilder
        {
            get { return instance.Builder; }
        }
        
        /// <summary>
        /// Gets/sets the builder.
        /// Setting the builder should only be done by NServiceBus framework code.
        /// </summary>
        public IBuilder Builder { get; set; }

        /// <summary>
        /// Gets/sets the object used to configure components.
        /// This object should eventually reference the same container as the Builder.
        /// </summary>
        public IConfigureComponents Configurer { get; set; }

        /// <summary>
        /// Protected constructor to enable creation only via the With method.
        /// </summary>
        protected Configure() { }

        /// <summary>
        /// Creates a new configuration object.
        /// </summary>
        /// <returns></returns>
        public static Configure With()
        {
            instance = new Configure();
            return instance;
        }

        /// <summary>
        /// Provides an instance to a startable bus.
        /// </summary>
        /// <returns></returns>
        public IStartableBus CreateBus()
        {
            return Builder.Build<IStartableBus>();
        }

        private static Configure instance;
    }
}
