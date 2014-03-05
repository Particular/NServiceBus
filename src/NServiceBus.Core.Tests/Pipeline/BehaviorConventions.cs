namespace NServiceBus.Core.Tests.Pipeline
{
    using System;
    using System.ComponentModel;
    using System.Linq;
    using NServiceBus.Pipeline;
    using NUnit.Framework;

    [TestFixture]
    public class BehaviorConventions
    {
        [Test]
        public void Verify()
        {
            var allBehaviors = typeof(IBehavior<>).Assembly.GetTypes()
                .Where(x => x.GetInterfaces().Any(y => y.Name.Contains("IBehavior")));
            foreach (var behavior in allBehaviors)
            {
                Assert.IsTrue(behavior.IsPublic, behavior + " should be public");
                var customAttributes = behavior.GetCustomAttributes(typeof(ObsoleteAttribute),false);
                Assert.IsNotEmpty(customAttributes, behavior + " should be marked with an ObsoleteAttribute");
                var obsoleteAttribute = (ObsoleteAttribute)customAttributes.First();
                Assert.AreEqual("This is a prototype API. May change in minor version releases.", obsoleteAttribute.Message, behavior + " should be marked with an ObsoleteAttribute");
                var editorAttributes = behavior.GetCustomAttributes(typeof(EditorBrowsableAttribute), false);
                Assert.IsNotEmpty(editorAttributes, behavior + " should be marked with an EditorBrowsableAttribute");
                var editorBrowsableAttribute = (EditorBrowsableAttribute)editorAttributes.First();
                Assert.AreEqual(EditorBrowsableState.Never, editorBrowsableAttribute.State, behavior + " should be marked with an EditorBrowsableAttribute");
            }
        }
    }
}