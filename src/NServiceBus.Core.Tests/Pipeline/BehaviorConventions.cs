namespace NServiceBus.Core.Tests.Pipeline
{
    using System;
    using System.ComponentModel;
    using System.Diagnostics;
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
                Debug.WriteLine(behavior);
                Assert.IsTrue(behavior.IsPublic);
                var obsoleteAttribute = (ObsoleteAttribute)behavior.GetCustomAttributes(typeof(ObsoleteAttribute),false).First();
                Assert.AreEqual("This is a prototype API. May change in minor version releases.", obsoleteAttribute.Message);
                var editorBrowsableAttribute = (EditorBrowsableAttribute)behavior.GetCustomAttributes(typeof(EditorBrowsableAttribute),false).First();
                Assert.AreEqual(EditorBrowsableState.Never, editorBrowsableAttribute.State);
            }
        }
    }
}