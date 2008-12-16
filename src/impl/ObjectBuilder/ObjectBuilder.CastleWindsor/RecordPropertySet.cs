using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Castle.Core.Interceptor;

namespace ObjectBuilder.CastleWindsor
{
    public class RecordPropertySet : IInterceptor
    {
        private readonly IComponentConfig componentConfig;

        public RecordPropertySet(IComponentConfig componentConfig)
        {
            this.componentConfig = componentConfig;
        }

        public void Intercept(IInvocation invocation)
        {
            if (invocation.Method.Name.StartsWith("set_"))
            {
                componentConfig.ConfigureProperty(invocation.Method.Name.Substring(4), invocation.Arguments[0]);
            }
        }
    }
}
