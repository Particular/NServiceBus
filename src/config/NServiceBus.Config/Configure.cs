using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ObjectBuilder;

namespace NServiceBus
{
    public class Configure
    {
        public IBuilder Builder { get; set; }
        public IConfigureComponents Configurer { get; set; }

        protected Configure() { }

        public static Configure With()
        {
            return new Configure();
        }

        public IStartableBus CreateBus()
        {
            return Builder.Build<IStartableBus>();
        }
    }
}
