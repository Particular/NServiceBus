using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Castle.Core.Interceptor;
using NServiceBus.ObjectBuilder;

namespace NServiceBus.ObjectBuilder.CastleWindsor
{
    /// <summary>
    /// Implementation of IInterceptor for capturing calls to the proxy.
    /// </summary>
    public class RecordPropertySet : IInterceptor
    {
        private readonly IComponentConfig componentConfig;

        /// <summary>
        /// Instantiates the class saving a reference to the given component config.
        /// </summary>
        /// <param name="componentConfig"></param>
        public RecordPropertySet(IComponentConfig componentConfig)
        {
            this.componentConfig = componentConfig;
        }

        /// <summary>
        /// When intercepting the given invocation, checks to see if it is a property set,
        /// and if so, stores the parameter of the invocation in the previously provided
        /// component config.
        /// </summary>
        /// <param name="invocation"></param>
        public void Intercept(IInvocation invocation)
        {
            if (invocation.Method.Name.StartsWith("set_"))
            {
                componentConfig.ConfigureProperty(invocation.Method.Name.Substring(4), invocation.Arguments[0]);
            }
        }
    }
}
