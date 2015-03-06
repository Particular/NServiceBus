namespace NServiceBus.Core.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using System.Runtime.Serialization;
    using NUnit.Framework;
    using Unicast;

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
                Assert.IsTrue(exceptionType.IsPublic, string.Format("Exception '{0}' should be public", exceptionType.Name));
                var constructor = exceptionType.GetConstructor(BindingFlags.NonPublic | BindingFlags.CreateInstance | BindingFlags.Instance, null, new[] { typeof(SerializationInfo), typeof(StreamingContext) }, null);
                Assert.IsNotNull(constructor, string.Format("Exception '{0}' should implement 'protected {0}(SerializationInfo info, StreamingContext context){{}}'", exceptionType.Name));
                var serializableAttribute = exceptionType.GetCustomAttributes(typeof(SerializableAttribute), false).FirstOrDefault();
                Assert.IsNotNull(serializableAttribute, string.Format("Exception '{0}' should have a 'SerializableAttribute'", exceptionType.Name));
                var properties = exceptionType.GetProperties(BindingFlags.Public|BindingFlags.Instance|BindingFlags.DeclaredOnly);
                if (properties.Length > 0)
                {
                    var getObjectDataMethod = exceptionType.GetMethod("GetObjectData");
                    Assert.IsTrue(getObjectDataMethod.DeclaringType.Name != "Exception", string.Format("Exception '{0}' has properties and as such should override 'GetObjectData'", exceptionType.Name));
                }
            }
        }

        static IEnumerable<Type> GetExceptionTypes()
        {
            foreach (var type in typeof(UnicastBus).Assembly.GetTypes())
            {
                if (typeof(Exception).IsAssignableFrom(type) && type.Namespace.StartsWith("NServiceBus"))
                {
                    yield return type;
                }
            }
        }
    }
}