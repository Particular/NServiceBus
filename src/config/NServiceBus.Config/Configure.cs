using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ObjectBuilder;

namespace NServiceBus
{
    public class Configure
    {
        public static IBuilder ObjectBuilder
        {
            get { return instance.Builder; }
        }
        
        public IBuilder Builder { get; set; }
        public IConfigureComponents Configurer { get; set; }

        protected Configure() { }

        public static Configure With()
        {
            instance = new Configure();
            return instance;
        }

        public IStartableBus CreateBus()
        {
            return Builder.Build<IStartableBus>();
        }

        private static Configure instance;
    }
}
