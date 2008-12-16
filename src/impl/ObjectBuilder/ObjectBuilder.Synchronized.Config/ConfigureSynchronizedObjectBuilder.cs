using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NServiceBus.ObjectBuilder.Common;
using NServiceBus.ObjectBuilder.Synchronized;

namespace NServiceBus
{
    public static class ConfigureSynchronizedObjectBuilder
    {
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
