namespace NServiceBus.Core.Tests.Pipeline
{
    using System;
    using NServiceBus.Pipeline;
    using NServiceBus.Pipeline.Contexts;
    using NUnit.Framework;

    [TestFixture]
    public class BehaviorTypeCheckerTests
    {
        [Test]
        public void Should_not_throw_for_simple_behavior()
        {
            BehaviorTypeChecker.ThrowIfInvalid(typeof(ValidBehavior), "foo");
        }

        class ValidBehavior : Behavior<RootContext>
        {
            public override void Invoke(RootContext context, Action next)
            {
            }
        }

        [Test]
        public void Should_not_throw_for_closed_generic_behavior()
        {
            BehaviorTypeChecker.ThrowIfInvalid(typeof(GenericBehavior<object>), "foo");
        }

        [Test]
        public void Should_throw_for_non_behavior()
        {
            Assert.Throws<ArgumentException>(() => BehaviorTypeChecker.ThrowIfInvalid(typeof(string), "foo"));
        }

        [Test]
        public void Should_throw_for_open_generic_behavior()
        {
            Assert.Throws<ArgumentException>(() => BehaviorTypeChecker.ThrowIfInvalid(typeof(GenericBehavior<>), "foo"));
        }

        class GenericBehavior<T> : Behavior<RootContext>
        {
            public override void Invoke(RootContext context, Action next)
            {
            }
        }
    }
}