namespace NServiceBus.Core.Utils.Reflection;

using System;
using NUnit.Framework;

[TestFixture]
public class InspectTests
{
    [TestFixture]
    public class When_is_a_simple_property
    {

        [Test]
        public void Should_return_property_name()
        {
            var propertyInfo = Inspect<Target>.GetProperty(target => target.Property);
            Assert.That(propertyInfo.Name, Is.EqualTo("Property"));
        }

        public class Target
        {
            public string Property { get; set; }
        }
    }

    [TestFixture]
    public class When_is_a_field
    {

        [Test]
        public void Should_throw()
        {
            var argumentException = Assert.Throws<ArgumentException>(() => Inspect<Target>.GetProperty(target => target.Field));
            Assert.That(argumentException.Message, Is.EqualTo("Member is not a property"));
        }

        public class Target
        {
            public string Field;
        }
    }

    [TestFixture]
    public class When_is_a_nested_property
    {

        [Test]
        public void Should_return_property_name()
        {
            var propertyInfo = Inspect<Target1>.GetProperty(target => target.Property1.Property2);
            Assert.That(propertyInfo.Name, Is.EqualTo("Property2"));
        }

        [Test]
        public void Should_throw_when_dots_not_allowed()
        {
            var argumentException = Assert.Throws<ArgumentException>(() => Inspect<Target1>.GetProperty(target => target.Property1.Property2, true));
            Assert.That(argumentException.Message, Does.StartWith("Argument passed contains more than a single dot which is not allowed: target => target.Property1.Property2"));
            Assert.That(argumentException.ParamName, Is.EqualTo("member"));
        }

        public class Target1
        {
            public Target2 Property1 { get; set; }
        }

        public class Target2
        {
            public string Property2 { get; set; }
        }
    }
}