using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NServiceBus.ObjectBuilder.Common;
using NServiceBus.ObjectBuilder.Synchronized;

namespace NServiceBus
{
    /// <summary>
    /// Contains extension methods for NServiceBus.Configure.
    /// </summary>
    public static class ConfigureSynchronizedObjectBuilder
    {
        /// <summary>
        /// Use this for smart client applications to provide thread-safety facilities.
        /// </summary>
        /// <param name="config"></param>
        /// <returns></returns>
        public static Configure SynchronizedBuilder(this Configure config)
        {
            if (config.Builder != null)
                throw new InvalidOperationException("SynchronizedBuilder must be called before all other builders.");

            var b = new CommonObjectBuilder();

            config.Builder = b;
            config.Configurer = b;

            b.Builder = new SynchronizedObjectBuilder();

            return config;
        }
    }
}
