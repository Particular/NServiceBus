using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Castle.MicroKernel;

namespace ObjectBuilder.CastleWindsor
{
    public class ConfigureComponentAdapter : IComponentConfig
    {
        private readonly IHandler handler;

        public ConfigureComponentAdapter(IHandler handler)
        {
            this.handler = handler;
        }

        public IComponentConfig ConfigureProperty(string name, object value)
        {
            handler.AddCustomDependencyValue(name, value);
            return this;
        }
    }
}
