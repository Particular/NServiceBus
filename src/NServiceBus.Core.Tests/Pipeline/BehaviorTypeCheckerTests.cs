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
            BehaviorTypeChecker.ThrowIfInvalid(typeof(ValidBehavior));
        }

        class ValidBehavior : IBehavior<RootContext>
        {
            public void Invoke(RootContext context, Action next)
            {
            }
        }

        [Test]
        public void Should_not_throw_for_closed_generic_behavior()
        {
            BehaviorTypeChecker.ThrowIfInvalid(typeof(GenericBehavior<object>));
        }

        [Test]
        public void Should_throw_for_non_behavior()
        {
            var exception = Assert.Throws<Exception>(() => BehaviorTypeChecker.ThrowIfInvalid(typeof(string)));
            Assert.AreEqual("The behavior 'String' is invalid since it does not implement IBehavior<T>.", exception.Message);
        }

        [Test]
        public void Should_throw_for_open_generic_behavior()
        {
            var exception = Assert.Throws<Exception>(() => BehaviorTypeChecker.ThrowIfInvalid(typeof(GenericBehavior<>)));
            Assert.AreEqual("The behavior 'GenericBehavior`1' is invalid since it is an open generic.", exception.Message);
        }

        class GenericBehavior<T> : IBehavior<RootContext>
        {
            public void Invoke(RootContext context, Action next)
            {
            }
        }

        [Test]
        public void Should_throw_for_abstract_behavior()
        {
            var exception = Assert.Throws<Exception>(() => BehaviorTypeChecker.ThrowIfInvalid(typeof(AbstractBehavior)));
            Assert.AreEqual("The behavior 'AbstractBehavior' is invalid since it is abstract.", exception.Message);
        }

        abstract class AbstractBehavior : IBehavior<RootContext>
        {
            public void Invoke(RootContext context, Action next)
            {
            }
        }
    }
}