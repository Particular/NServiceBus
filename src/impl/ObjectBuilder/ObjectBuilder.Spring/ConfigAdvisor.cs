using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Spring.Aop.Support;
using Spring.Aop;
using ObjectBuilder;

namespace NServiceBus.ObjectBuilder.Spring
{
    public class ConfigAdvisor : DefaultPointcutAdvisor
    {
        public ConfigAdvisor(Type type, IComponentConfig config)
            : base(new SetterPointcut(type), new ConfigAdvice(config))
        { }

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
