namespace ObjectBuilder.Tests
{
    using System;
    using NServiceBus;
    using NServiceBus.ObjectBuilder.Autofac;
    using NUnit.Framework;

    [TestFixture]
    public class When_releasing_components : BuilderFixture
    {
        [Test]
        public void Transient_component_should_be_disposed_and_destructor_called()
        {
            ForAllBuilders(builder =>
                {
                    builder.Configure(typeof (TransientClass), DependencyLifecycle.InstancePerCall);

                    var comp = (TransientClass) builder.Build(typeof (TransientClass));
                    comp.Name = "Jon";

                    var weak = new WeakReference(comp);

                    builder.Release(comp);

                    comp = null;

                    GC.Collect();
                    GC.WaitForPendingFinalizers();

                    Assert.IsFalse(weak.IsAlive);
                }, typeof(AutofacObjectBuilder));
        }

        public class TransientClass : IDisposable
        {
            bool disposed;

            public string Name { get; set; }

            ~TransientClass()
            {
                Dispose(false);
            }

            public void Dispose()
            {
                Dispose(true);
                GC.SuppressFinalize(this);
            }

            protected virtual void Dispose(bool disposing)
            {
                if (disposed)
                {
                    return;
                }

                if (disposing)
                {

                }

                disposed = true;
            }
        }
    }
}