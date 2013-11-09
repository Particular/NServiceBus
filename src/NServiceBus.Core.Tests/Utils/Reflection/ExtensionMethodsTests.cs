namespace NServiceBus.Core.Utils.Reflection
{
    using System;
    using System.Diagnostics;
    using NServiceBus.Utils.Reflection;
    using NUnit.Framework;

    [TestFixture]
    public class ExtensionMethodsTests
    {
        [TestFixture]
        public class Issue_1630
        {
            [Test]
            public void Should_return_return_different_results_for_different_types()
            {
                // This test verifies whether the added cache doesn't break the execution if called successively for two different types

                var customTypeResult = typeof(Target).IsSystemType();
                var systemTypeResult = typeof(string).IsSystemType();

                Assert.IsTrue(systemTypeResult, "Expected string to be a system type.");
                Assert.IsFalse(customTypeResult, "Expected Target to be a custom type.");
            }

            public class Target
            {
                public string Property1 { get; set; }
            }
        }
    }
}
