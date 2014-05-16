
namespace NServiceBus.Core.Tests.Pipeline
{
    using System;
    using NServiceBus.Pipeline;
    using NServiceBus.Pipeline.Contexts;
    using NUnit.Framework;

    [TestFixture]
    class PipelineBuilderTests
    {
        [Test]
        public void Foo()
        {
            //var f = new BehaviorRegistrationsCoordinator();

            //f.Register("1", typeof(FakeBehavior), "1");
            //f.Register("2", typeof(FakeBehavior), "2");
            //f.Register("3", typeof(FakeBehavior), "3");

            //f.Register<MyCustomRegistration>();

            ////f.Remove("2");

            //var finalOrder = f.BuildRuntimeModel();

            //Assert.AreEqual(4, finalOrder.Count);

            //foreach (var metadata in finalOrder)
            //{
            //    Console.Out.WriteLine(metadata.Id);
            //}
        }

        class MyCustomRegistration : RegisterBehavior
        {
            public MyCustomRegistration()
                : base("1.5", typeof(FakeBehavior), "1.5")
            {
                InsertAfter("3");
                InsertAfter("2");
            }
        }
        class FakeBehavior:IBehavior<IncomingContext>
        {
            public void Invoke(IncomingContext context, Action next)
            {
                throw new NotImplementedException();
            }
        }
    }
}
