namespace NServiceBus.Core.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using NUnit.Framework;
    using Unicast;

    [TestFixture]
    public class AttributeTests
    {
        [Test]
        public void VerifyAttributesAreSealed()
        {
            var attributeTypes = new List<Type>();

            attributeTypes.AddRange(GetAttributeTypes(typeof(IMessage).Assembly));
            attributeTypes.AddRange(GetAttributeTypes(typeof(UnicastBus).Assembly));

            foreach (var attributeType in attributeTypes)
            {
                Assert.IsTrue(attributeType.IsSealed, attributeType.FullName+ " should be sealed.");
            }
        }
        static IEnumerable<Type> GetAttributeTypes(Assembly assembly)
        {
            return assembly.GetTypes()
                .Where(type => 
                    typeof(Attribute).IsAssignableFrom(type) && 
                    //TODO: remove when gitversion is updated
                    !type.Name.EndsWith("ReleaseDateAttribute") &&
                    !type.Name.EndsWith("NugetVersionAttribute"));
        }
    }
}