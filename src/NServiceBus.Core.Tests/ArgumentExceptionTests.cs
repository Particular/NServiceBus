namespace NServiceBus.Core.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Reflection;
    using System.Runtime.Serialization;
    using NUnit.Framework;

    [TestFixture]
    public class ArgumentExceptionTests
    {
        [Test]
        [Explicit]
        public void VerifyArgumentExceptions()
        {
            foreach (var type in GetPublicClasses())
            {
                var instance = FormatterServices.GetUninitializedObject(type);
                foreach (var methodInfo in type.GetMethods(BindingFlags.Instance|BindingFlags.Public))
                {
                    if (methodInfo.ContainsGenericParameters)
                    {
                        continue;
                    }
                    if (methodInfo.Name.StartsWith("get_"))
                    {
                        continue;
                    }
                    if (methodInfo.GetCustomAttribute<ObsoleteAttribute>() != null)
                    {
                        continue;
                    }
                    var parameters = new object[methodInfo.GetParameters().Length];
                    try
                    {
                        methodInfo.Invoke(instance, parameters);
                    }
                    catch (TargetInvocationException invocationException)
                    {
                        var innerException = invocationException.InnerException;
                        if (innerException is NullReferenceException)
                        {
                            Debug.WriteLine(type.Name+ "."+ methodInfo.Name + " NullRef");
                            continue;
                        }
                        if (innerException is ArgumentException)
                        {
                            continue;
                        }
                        Debug.WriteLine(type.Name + "." + methodInfo.Name + " " +innerException.GetType());
                    }
                }
            }

        }

        static IEnumerable<Type> GetPublicClasses()
        {
            return typeof(IMessage).Assembly.GetTypes()
                .Where(type => 
                    type.IsClass && 
                    !type.IsAbstract && 
                    !type.ContainsGenericParameters &&
                    type.GetCustomAttribute<ObsoleteAttribute>() == null &&
                    type.IsPublic && 
                    type.Namespace.StartsWith("NServiceBus"));
        }
    }
}