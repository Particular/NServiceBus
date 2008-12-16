using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using AopAlliance.Intercept;
using System.Collections;
using Common.Logging;
using ObjectBuilder;

namespace NServiceBus.ObjectBuilder.Spring
{
    public class ConfigAdvice : IMethodInterceptor
    {
        private readonly IComponentConfig config;

        public ConfigAdvice(IComponentConfig config)
        {
            this.config = config;
        }

        public virtual Object Invoke(IMethodInvocation invocation)
        {
            MethodInfo method = invocation.Method;

            if (IsSetter(method))
            {
                string name = GetName(invocation.Method.Name);
                config.ConfigureProperty(name, invocation.Arguments[0]);

                string message = method.DeclaringType.Name + "." + name + " = ";
                if (invocation.Arguments[0] is IEnumerable && !(invocation.Arguments[0] is string))
                {
                    message += "{";

                    foreach (object o in (IEnumerable)invocation.Arguments[0])
                        if (o is DictionaryEntry)
                            message += "<" + ((DictionaryEntry)o).Key + ", " + ((DictionaryEntry)o).Value + ">, ";
                        else
                            message += o + ", ";

                    message += "}";
                }
                else message += invocation.Arguments[0];

                LogManager.GetLogger(typeof(IBuilder)).Debug(message);
            }

            return null;
        }

        private static bool IsSetter(MethodInfo method)
        {
            return (method.Name.StartsWith("set_")) && (method.GetParameters().Length == 1);
        }

        private static string GetName(string setterName)
        {
            return setterName.Replace("set_", "");
        }
    }
}
