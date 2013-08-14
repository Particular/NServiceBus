namespace NServiceBus.Core.Tests.Sagas
{
    using System;
    using System.Linq;
    using NUnit.Framework;
    using Saga;

    [TestFixture]
    public class UniqueAttributeTests
    {

        [Test]
        public void Ensure_inherited_property_is_returned_when_attribute_exists()
        {
            var uniqueProperties = UniqueAttribute.GetUniqueProperties(typeof(InheritedModel))
                .ToList();
            Assert.AreEqual(1,uniqueProperties.Count);
            Assert.AreEqual("PropertyWithAttribute", uniqueProperties.First().Name);
        }

        [Test]
        public void EnsureOverridePropertyIsReturnedWhenAttributeExists()
        {
            var uniqueProperties = UniqueAttribute.GetUniqueProperties(typeof(InheritedModelWithOverride))
                .ToList();
            Assert.AreEqual(1,uniqueProperties.Count);
            Assert.AreEqual("PropertyWithAttribute", uniqueProperties.First().Name);
        }

        [Test]
        public void Ensure_property_is_returned_when_attribute_exists()
        {
            var uniqueProperties = UniqueAttribute.GetUniqueProperties(typeof(ModelWithUniqueProperty))
                .ToList();
            Assert.AreEqual(1,uniqueProperties.Count);
            Assert.AreEqual("PropertyWithAttribute", uniqueProperties.First().Name);
        }

        [Test]
        public void Ensure_multiple_properties_are_returned_when_multiple_attributes_exists()
        {
            var uniqueProperties = UniqueAttribute.GetUniqueProperties(typeof(ModelWithMultipleUniqueProperty))
                .ToList();
            Assert.AreEqual(2,uniqueProperties.Count);
            Assert.AreEqual("PropertyWithAttribute1", uniqueProperties.First().Name);
            Assert.AreEqual("PropertyWithAttribute2", uniqueProperties.Skip(1).First().Name);
        }

        [Test]
        public void Ensure_exception_is_thrown_when_multiple_attributes_exists()
        {
            Assert.Throws<InvalidOperationException>(() => UniqueAttribute.GetUniqueProperty(typeof(ModelWithMultipleUniqueProperty)));
        }

        [Test]
        public void Ensure_single_property_is_returned_when_attribute_exists()
        {
            var uniqueProperty = UniqueAttribute.GetUniqueProperty(typeof(ModelWithUniqueProperty));
            Assert.IsNotNull(uniqueProperty);
        }

        [Test]
        public void Ensure_property_is_returned_when_no_attribute_exists()
        {
            var uniqueProperties = UniqueAttribute.GetUniqueProperties(typeof(ModelWithNoUniqueProperty));
            Assert.IsEmpty(uniqueProperties);
        }

        [Test]
        public void Ensure_null_returned_when_no_attributes_exists()
        {
            var uniqueProperty = UniqueAttribute.GetUniqueProperty(typeof(ModelWithNoUniqueProperty));
            Assert.IsNull(uniqueProperty);
        }

        public class ModelWithMultipleUniqueProperty
        {
            [UniqueAttribute]
            public string PropertyWithAttribute1 { get; set; }   
            [UniqueAttribute]
            public string PropertyWithAttribute2 { get; set; }   
        }

        public class ModelWithUniqueProperty
        {
            [UniqueAttribute]
            public virtual string PropertyWithAttribute { get; set; }   
            public string PropertyWithNoAttribute { get; set; }   
        }
        public class InheritedModelWithOverride : ModelWithUniqueProperty
        {
            public override string PropertyWithAttribute { get; set; }
        }
        public class InheritedModel : ModelWithUniqueProperty
        {
        }
        public class ModelWithNoUniqueProperty
        {
            public string PropertyWithAttribute { get; set; }   
            public string PropertyWithNoAttribute { get; set; }   
        }
    }
}