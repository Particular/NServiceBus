using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Spring.Aop.Support;
using Spring.Aop;
using ObjectBuilder;

namespace NServiceBus.ObjectBuilder.Spring
{
    /// <summary>
    /// Used to perform AOP interception as a pointcut advisor
    /// </summary>
    public class ConfigAdvisor : DefaultPointcutAdvisor
    {
        /// <summary>
        /// Passes the setter pointcut and ConfigAdvice to the base DefaultPointcutAdvisor.
        /// </summary>
        /// <param name="type"></param>
        /// <param name="config"></param>
        public ConfigAdvisor(Type type, IComponentConfig config)
            : base(new SetterPointcut(type), new ConfigAdvice(config))
        { }

        /// <summary>
        /// Pointcut that works on methods that are property setters.
        /// </summary>
        private class SetterPointcut : IPointcut
        {
            private IMethodMatcher methodMatcher = TrueMethodMatcher.True;
            private ITypeFilter typeFilter;

            public SetterPointcut(Type type)
            {
                typeFilter = new RootTypeFilter(type);
            }

            public ITypeFilter TypeFilter
            {
                get { return typeFilter; }
            }

            public IMethodMatcher MethodMatcher
            {
                get { return methodMatcher; }
            }
        }
    }
}
