namespace NServiceBus.Core.Tests.Pipeline
{
    using System;
    using NServiceBus.Core.Tests.Features;
    using NServiceBus.Pipeline;
    using NServiceBus.Pipeline.Contexts;
    using NUnit.Framework;

    [TestFixture]
    class PipelineExecutorTests
    {
        [Test]
        public void Static_behaviors_are_shared_between_executions()
        {
            var builder = new FakeBuilder(typeof(SumBehavior));
            var modifications = new PipelineModifications();
            var settings = new PipelineSettings(modifications);
            settings.Register("Static", typeof(SumBehavior), "A static behavior", true);
            var executor = new PipelineExecutor(builder, new BusNotifications(), modifications, new BehaviorContextStacker(builder));

            var ctx1 = new IncomingContext(new RootContext(builder));
            ctx1.Set("Value",2);
            executor.InvokeReceivePipeline(ctx1);

            var ctx2 = new IncomingContext(new RootContext(builder));
            ctx2.Set("Value", 3);
            executor.InvokeReceivePipeline(ctx2);

            var sum = ctx2.Get<int>("Sum");

            Assert.AreEqual(5, sum);
        }
        
        [Test]
        public void Non_static_behaviors_are_not_shared_between_executions()
        {
            var builder = new FakeBuilder(typeof(SumBehavior));
            var modifications = new PipelineModifications();
            var settings = new PipelineSettings(modifications);
            settings.Register("NonStatic", typeof(SumBehavior), "A non-static behavior", false);
            var executor = new PipelineExecutor(builder, new BusNotifications(), modifications, new BehaviorContextStacker(builder));

            var ctx1 = new IncomingContext(new RootContext(builder));
            ctx1.Set("Value",2);
            executor.InvokeReceivePipeline(ctx1);

            var ctx2 = new IncomingContext(new RootContext(builder));
            ctx2.Set("Value", 3);
            executor.InvokeReceivePipeline(ctx2);

            var sum = ctx2.Get<int>("Sum");

            Assert.AreEqual(3, sum);
        }

        class SumBehavior : Behavior<IncomingContext>
        {
            int sum;

            public override void Invoke(IncomingContext context, Action next)
            {
                var value = context.Get<int>("Value");
                sum += value;
                context.Set("Sum", sum);
                next();
            }
        }
    }
}