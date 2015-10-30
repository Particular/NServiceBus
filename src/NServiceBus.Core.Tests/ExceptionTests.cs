﻿namespace NServiceBus.Core.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using System.Runtime.Serialization;
    using NUnit.Framework;

    [TestFixture]
    public class ExceptionTests
    {
        [Test]
        public void VerifyExceptionConventions()
        {
            foreach (var exceptionType in GetExceptionTypes())
            {
                if (exceptionType.GetCustomAttribute<ObsoleteAttribute>() !=null )
                {
                    continue;
                }
                if (!exceptionType.IsPublic)
                {
                    continue;
                }
                var constructor = exceptionType.GetConstructor(BindingFlags.NonPublic | BindingFlags.CreateInstance | BindingFlags.Instance, null, new[] { typeof(SerializationInfo), typeof(StreamingContext) }, null);
                Assert.IsNotNull(constructor, string.Format("Exception '{0}' should implement 'protected {0}(SerializationInfo info, StreamingContext context){{}}'", exceptionType.Name));
                var serializableAttribute = exceptionType.GetCustomAttributes(typeof(SerializableAttribute), false).FirstOrDefault();
                Assert.IsNotNull(serializableAttribute, $"Exception '{exceptionType.Name}' should have a 'SerializableAttribute'");
                var properties = exceptionType.GetProperties(BindingFlags.Public|BindingFlags.Instance|BindingFlags.DeclaredOnly);
                if (properties.Length > 0)
                {
                    var getObjectDataMethod = exceptionType.GetMethod("GetObjectData");
                    Assert.IsTrue(getObjectDataMethod.DeclaringType.Name != "Exception", $"Exception '{exceptionType.Name}' has properties and as such should override 'GetObjectData'");
                }
            }
        }

        static IEnumerable<Type> GetExceptionTypes()
        {
            foreach (var type in typeof(IBusInterface).Assembly.GetTypes())
            {
                if (typeof(Exception).IsAssignableFrom(type) && type.Namespace.StartsWith("NServiceBus"))
                {
                    yield return type;
                }
            }
        }
    }
}