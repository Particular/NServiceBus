using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ObjectBuilder;

namespace NServiceBus.ObjectBuilder.Common.Config
{
    public static class ConfigureCommon
    {
        public static void With<T>(Configure config) where T : IBuilderInternal, new()
        {
            if (config.Builder == null)
            {
                var b = new CommonObjectBuilder();

                config.Builder = b;
                config.Configurer = b;
            }

            var containBuilder = config.Builder as IContainInternalBuilder;
            if (containBuilder != null)
            {
                var internalContainer = containBuilder.Builder as IContainInternalBuilder;
                if (internalContainer != null)
                {
                    internalContainer.Builder = new T();
                }
                else
                {
                    if (containBuilder.Builder != null)
                        throw new InvalidOperationException("Builder already configured.");

                    T builder = new T();

                    containBuilder.Builder = builder;
                }

                config.Configurer.ConfigureComponent<CommonObjectBuilder>(ComponentCallModelEnum.Singleton)
                    .Builder = containBuilder.Builder;
            }
        }
    }
}
