namespace NServiceBus.TransportTests
{
    using System;
    using System.Linq;
    using NUnit.Framework;

    [TestFixture]
    public class StandardsTests
    {
        [Test]
        public void Each_test_class_contains_single_test_case()
        {
            var assembly = typeof(StandardsTests).Assembly;

            var testTypes = assembly.GetTypes().Where(x => x.Name.StartsWith("When_"));

            foreach (Type testType in testTypes)
            {
                var testMethods = testType.GetMethods()
                    .Where(m => m.GetCustomAttributes(false).Any(a => a is TestCaseAttribute || a is TestAttribute));

                if (testMethods.Count() > 1)
                {
                    Assert.Fail($"Test {testType.Name} contains more than one test method. Multiple test methods are not allowed because transports don't support cleanup between running these methods.");
                }
            }
        }
    }
}