namespace NServiceBus.Core.Tests.Recoverability
{
    using System;
    using NUnit.Framework;
    using Settings;

    [TestFixture]
    public class RecoverabilitySettingsExtensionsTests
    {
        [Test]
        public void When_no_unrecoverable_exception_present_should_add_exception_type()
        {
            var settings = new SettingsHolder();
            settings.AddUnrecoverableException(typeof(Exception));

            var result = settings.UnrecoverableExceptions();

            Assert.That(result, Has.Count.EqualTo(1));
            Assert.IsTrue(result.Contains(typeof(Exception)));
        }

        [Test]
        public void When_unrecoverable_exception_present_should_add_exception_type()
        {
            var settings = new SettingsHolder();
            settings.AddUnrecoverableException(typeof(Exception));
            settings.AddUnrecoverableException(typeof(InvalidOperationException));

            var result = settings.UnrecoverableExceptions();

            Assert.That(result, Has.Count.EqualTo(2));
            Assert.IsTrue(result.Contains(typeof(InvalidOperationException)));
        }

        [Test]
        public void When_adding_two_times_the_same_type_should_deduplicate()
        {
            var settings = new SettingsHolder();
            settings.AddUnrecoverableException(typeof(Exception));
            settings.AddUnrecoverableException(typeof(Exception));

            var result = settings.UnrecoverableExceptions();

            Assert.That(result, Has.Count.EqualTo(1));
            Assert.IsTrue(result.Contains(typeof(Exception)));
        }
    }
}