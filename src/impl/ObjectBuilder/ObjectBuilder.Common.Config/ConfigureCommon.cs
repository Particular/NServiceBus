using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ObjectBuilder;

namespace NServiceBus.ObjectBuilder.Common.Config
{
    /// <summary>
    /// Utility configuration class for implementers of IBuilderInternal.
    /// </summary>
    public static class ConfigureCommon
    {
        /// <summary>
        /// Sets the Builder property of the given Configure object to an instance of CommonObjectBuilder.
        /// Then, the given builder object is inserted in the relevant place of the builder chain.
        /// Finally, the given actions are performed on the instance of CommonObjectBuilder.
        /// </summary>
        /// <param name="config"></param>
        /// <param name="builder"></param>
        /// <param name="configActions"></param>
        public static void With(Configure config, IBuilderInternal builder, params Action<IConfigureComponents>[] configActions)
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
                    internalContainer.Builder = builder;
                }
                else
                {
                    if (containBuilder.Builder != null)
                        throw new InvalidOperationException("Builder already configured.");

                    containBuilder.Builder = builder;
                }

                config.Configurer.ConfigureComponent<CommonObjectBuilder>(ComponentCallModelEnum.Singleton)
                    .Builder = containBuilder.Builder;

                foreach (var a in configActions)
                    a(config.Configurer);
            }
        }
    }
}
