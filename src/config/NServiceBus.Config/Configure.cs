using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ObjectBuilder;

namespace NServiceBus.Config
{
    public class Configure
    {
        public IBuilder builder;

        protected Configure() { }

        public static Configure With(IBuilder builder)
        {
            Configure result = new Configure();
            result.builder = builder;

            return result;
        }
    }
}
