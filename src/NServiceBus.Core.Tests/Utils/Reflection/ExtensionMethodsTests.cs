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
            public void Should_perform_fast_for_many_calls_with_same_type()
            {
                var stopWatch = new Stopwatch();

                var type = typeof(Target);
                var result = true;

                stopWatch.Start();

                for (int i = 0; i < 1000000; i++)
                {
                    result = type.IsSystemType();
                }

                stopWatch.Stop();

                Assert.LessOrEqual(stopWatch.ElapsedMilliseconds, 50, "Should perform in under 50ms.");
            }

            [Test]
            public void Should_return_return_different_results_for_different_types()
            {
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
