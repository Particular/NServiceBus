namespace NServiceBus.Core.Utils.Reflection
{
    using System;
    using NServiceBus.Utils.Reflection;
    using NUnit.Framework;

    [TestFixture]
    public class ReflectTests
    {
        [TestFixture]
        public class When_is_a_simple_property
        {

            [Test]
            public void Should_return_property_name()
            {
                var propertyInfo = Reflect<Target>.GetProperty(target => target.Property);
                Assert.AreEqual("Property", propertyInfo.Name);
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
                var argumentException = Assert.Throws<ArgumentException>(() => Reflect<Target>.GetProperty(target => target.Field));
                Assert.AreEqual("Member is not a property", argumentException.Message);
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
                var propertyInfo = Reflect<Target1>.GetProperty(target => target.Property1.Property2);
                Assert.AreEqual("Property2", propertyInfo.Name);
            }
            [Test]
            public void Should_thoww_when_dots_not_allowed()
            {
                var argumentException = Assert.Throws<ArgumentException>(() => Reflect<Target1>.GetProperty(target => target.Property1.Property2, true));
                Assert.AreEqual("Argument passed contains more than a single dot which is not allowed: target => target.Property1.Property2\r\nParameter name: member", argumentException.Message);
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

}