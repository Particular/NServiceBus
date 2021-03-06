namespace NServiceBus.TransportTests
{
    using System;
    using System.Linq;
    using System.Reflection;
    using NUnit.Framework;

    // Removed from the distributable package in the csproj file
    public class TransportTestPolicy
    {
        [TestCase]
        public void LimitOneTransportTestPerClass()
        {
            var multiTestClasses = typeof(TransportTestPolicy).Assembly.GetTypes()
                .Where(type => type != typeof(TransportTestPolicy))
                .SelectMany(type => type.GetMethods())
                .Where(IsTestMethod)
                .GroupBy(method => method.DeclaringType)
                .Where(group => group.Count() > 1)
                .Select(group => $"{Environment.NewLine}  - {group.Key.FullName}")
                .ToArray();

            Assert.IsEmpty(multiTestClasses, "Each transport test method should be in its own class. The class determines the queue name and can lead to subtle bugs between tests. Offenders:" + string.Join("", multiTestClasses));
        }

        static bool IsTestMethod(MethodInfo method) =>
            method.GetCustomAttributes<NUnitAttribute>().Any(att => att is TestAttribute || att is TestCaseAttribute);
    }
}
