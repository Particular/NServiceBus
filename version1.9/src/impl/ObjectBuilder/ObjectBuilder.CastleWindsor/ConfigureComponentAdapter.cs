using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Castle.MicroKernel;
using NServiceBus.ObjectBuilder;

namespace NServiceBus.ObjectBuilder.CastleWindsor
{
    /// <summary>
    /// Castle Windsor implementation of IComponentConfig.
    /// </summary>
    public class ConfigureComponentAdapter : IComponentConfig
    {
        private readonly IHandler handler;

        /// <summary>
        /// Instantiates the class and saves the given IHandler object.
        /// </summary>
        /// <param name="handler"></param>
        public ConfigureComponentAdapter(IHandler handler)
        {
            this.handler = handler;
        }

        /// <summary>
        /// Calls AddCustomDependencyValue on the previously given handler.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public IComponentConfig ConfigureProperty(string name, object value)
        {
            handler.AddCustomDependencyValue(name, value);
            return this;
        }
    }
}
