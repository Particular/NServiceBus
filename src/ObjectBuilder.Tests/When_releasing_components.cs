namespace ObjectBuilder.Tests
{
    using System;
    using NServiceBus;
    using NUnit.Framework;

    [TestFixture]
    public class When_releasing_components : BuilderFixture
    {
        [Test]
        public void Transient_component_should_be_destructed_called()
        {
            ForAllBuilders(builder =>
            {
                builder.Configure(typeof(TransientClass), DependencyLifecycle.InstancePerCall);

                var comp = (TransientClass) builder.Build(typeof(TransientClass));
                comp.Name = "Jon";

                var weak = new WeakReference(comp);

                builder.Release(comp);

                // ReSharper disable once RedundantAssignment
                comp = null;

                GC.Collect();
                GC.WaitForPendingFinalizers();

                Assert.IsFalse(weak.IsAlive);
                Assert.IsTrue(TransientClass.Destructed);
            });//Not supported by typeof(AutofacObjectBuilder));
        }

        public class TransientClass 
        {
            public static bool Destructed;

            public string Name { get; set; }

            ~TransientClass()
            {
                Destructed = true;
            }
        }
    }
}