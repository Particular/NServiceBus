namespace NServiceBus.Core.Tests
{
    using System;
    using NUnit.Framework;

    [TestFixture]
    public class DeepCopyTests
    {
        class SomethingWithFunc
        {
            public Func<Inner> Function { get; }

            public SomethingWithFunc(Func<Inner> function)
            {
                Function = function;
            }
        }

        class Other
        {
            public Inner Property { get; set; }
        }

        class Inner { }

        [Test]
        public void Should_copy_functions_with_closures()
        {
            var other = new Other { Property = new Inner() };
            var instance = new SomethingWithFunc(() => other.Property);
            var copy = instance.DeepCopy();

            Assert.IsNotNull(copy.Function);
            Assert.AreNotSame(instance.Function(), copy.Function());
        }

        [Test]
        public void Should_copy_static_functions()
        {
            var instance = new SomethingWithFunc(() => new Inner());
            var copy = instance.DeepCopy();

            Assert.IsNotNull(copy.Function);
            Assert.AreNotSame(instance.Function(), copy.Function());
        }

        static class WithStaticState
        {
            public static int State;
        }

        [Test]
        public void Should_copy_static_functions_with_state()
        {
            var instance = new SomethingWithFunc(() =>
            {
                WithStaticState.State++;
                return new Inner();
            });
            var copy = instance.DeepCopy();

            Assert.IsNotNull(copy.Function);
            Assert.AreNotSame(instance.Function(), copy.Function());
            Assert.AreEqual(2, WithStaticState.State);
        }
    }
}