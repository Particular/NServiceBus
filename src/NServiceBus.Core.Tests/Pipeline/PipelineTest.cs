namespace NServiceBus.Core.Tests.Pipeline
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Linq.Expressions;
    using System.Text;
    using System.Threading.Tasks;
    using ApprovalTests;
    using NServiceBus.Pipeline;
    using NUnit.Framework;
    using Settings;
    using Testing;
    using FakeBuilder = Testing.FakeBuilder;

    [TestFixture]
    public class PipelineTest
    {
        [Test]
        public async Task ShouldExecutePipeline()
        {
            var stringWriter = new StringWriter();

            var pipelineModifications = new PipelineModifications();
            pipelineModifications.Additions.Add(new Behavior1.Registration(stringWriter));
            pipelineModifications.Additions.Add(new Stage1.Registration(stringWriter));
            pipelineModifications.Additions.Add(new Behavior2.Registration(stringWriter));
            pipelineModifications.Additions.Add(new StageFork.Registration(stringWriter));

            var pipeline = new Pipeline<ITransportReceiveContext>(new FakeBuilder(), new SettingsHolder(), pipelineModifications);

            var context = new TestableTransportReceiveContext();
            context.Extensions.Set<IPipelineCache>(new FakePipelineCache());

            await pipeline.Invoke(context);

            Assert.AreEqual(@"stagefork1
behavior1
stage1
behavior2
", stringWriter.ToString());
        }

        [Test]
        public void ShouldCreateCachedExecutionPlan()
        {
            var stringWriter = new StringWriter();

            var behaviors = new IBehavior[]
            {
                new StageFork("stagefork1", stringWriter),
                new Behavior1("behavior1", stringWriter),
                new Stage1("stage1", stringWriter),
                new Behavior2("behavior2", stringWriter),
                new Stage2("stage2", stringWriter),
                new Terminator("terminator", stringWriter),
            };

            var expression = behaviors.CreatePipelineExecutionExpression();

            Approvals.Verify(expression.PrettyPrint());
        }

        [Test]
        public async Task ShouldCacheExecutionFunc()
        {
            var stringWriter = new StringWriter();

            var pipelineModifications = new PipelineModifications();
            pipelineModifications.Additions.Add(new Behavior1.Registration(stringWriter));
            pipelineModifications.Additions.Add(new Stage1.Registration(stringWriter));
            pipelineModifications.Additions.Add(new Behavior2.Registration(stringWriter));
            pipelineModifications.Additions.Add(new StageFork.Registration(stringWriter));

            var pipeline = new Pipeline<ITransportReceiveContext>(new FakeBuilder(), new SettingsHolder(), pipelineModifications);

            var context = new TestableTransportReceiveContext();
            context.Extensions.Set<IPipelineCache>(new FakePipelineCache());

            var stopwatch = Stopwatch.StartNew();
            await pipeline.Invoke(context);
            stopwatch.Stop();

            var firstRunTicks = stopwatch.ElapsedTicks;

            var runs = new List<long>();
            for (var i = 0; i < 100; i++)
            {
                stopwatch = Stopwatch.StartNew();
                await pipeline.Invoke(context);
                stopwatch.Stop();
                runs.Add(stopwatch.ElapsedTicks);
            }

            var average = runs.Average();

            Assert.That(average, Is.LessThan(firstRunTicks / 5));
        }

        class StageFork : StageForkConnector<ITransportReceiveContext, IIncomingPhysicalMessageContext, IBatchDispatchContext>
        {
            public StageFork(string instance, TextWriter writer)
            {
                this.instance = instance;
                this.writer = writer;
            }

            public override async Task Invoke(ITransportReceiveContext context, Func<IIncomingPhysicalMessageContext, Task> stage, Func<IBatchDispatchContext, Task> fork)
            {
                writer.WriteLine(instance);
                await stage(new TestableIncomingPhysicalMessageContext()).ConfigureAwait(false);
                var dispatchContext = new TestableBatchDispatchContext();
                dispatchContext.Extensions.Merge(context.Extensions);
                await fork(dispatchContext).ConfigureAwait(false);
            }

            readonly string instance;
            readonly TextWriter writer;

            public class Registration : RegisterStep
            {
                public Registration(TextWriter writer) : base("StageFork", typeof(StageFork), "StageFork", b => new StageFork("stagefork1", writer))
                {
                }
            }
        }

        class Behavior1 : Behavior<IIncomingPhysicalMessageContext>
        {
            public Behavior1(string instance, TextWriter writer)
            {
                this.instance = instance;
                this.writer = writer;
            }

            public override Task Invoke(IIncomingPhysicalMessageContext context, Func<Task> next)
            {
                writer.WriteLine(instance);
                return next();
            }

            readonly string instance;
            readonly TextWriter writer;

            public class Registration : RegisterStep
            {
                public Registration(TextWriter writer) : base("Behavior1", typeof(Behavior1), "Behavior1", b => new Behavior1("behavior1", writer))
                {
                }
            }
        }

        class Stage1 : StageConnector<IIncomingPhysicalMessageContext, IIncomingLogicalMessageContext>
        {
            public Stage1(string instance, TextWriter writer)
            {
                this.writer = writer;
                this.instance = instance;
            }

            public override Task Invoke(IIncomingPhysicalMessageContext context, Func<IIncomingLogicalMessageContext, Task> stage)
            {
                writer.WriteLine(instance);
                return stage(new TestableIncomingLogicalMessageContext());
            }

            string instance;
            TextWriter writer;

            public class Registration : RegisterStep
            {
                public Registration(TextWriter writer) : base("Stage1", typeof(Stage1), "Stage1", b => new Stage1("stage1", writer))
                {
                }
            }
        }

        class Behavior2 : Behavior<IIncomingLogicalMessageContext>
        {
            public Behavior2(string instance, TextWriter writer)
            {
                this.instance = instance;
                this.writer = writer;
            }

            public override Task Invoke(IIncomingLogicalMessageContext context, Func<Task> next)
            {
                writer.WriteLine(instance);
                return next();
            }

            readonly string instance;
            readonly TextWriter writer;

            public class Registration : RegisterStep
            {
                public Registration(TextWriter writer) : base("Behavior2", typeof(Behavior2), "Behavior2", b => new Behavior2("behavior2", writer))
                {
                }
            }
        }

        class Stage2 : StageConnector<IIncomingLogicalMessageContext, IDispatchContext>
        {
            public Stage2(string instance, TextWriter writer)
            {
                this.writer = writer;
                this.instance = instance;
            }

            public override Task Invoke(IIncomingLogicalMessageContext context, Func<IDispatchContext, Task> stage)
            {
                writer.WriteLine(instance);
                return stage(new TestableDispatchContext());
            }

            string instance;
            TextWriter writer;

            public class Registration : RegisterStep
            {
                public Registration(TextWriter writer) : base("Stage2", typeof(Stage2), "Stage2", b => new Stage2("stage2", writer))
                {
                }
            }
        }

        class Terminator : PipelineTerminator<IDispatchContext>
        {
            public Terminator(string instance, TextWriter writer)
            {
                this.instance = instance;
                this.writer = writer;
            }

            protected override Task Terminate(IDispatchContext context)
            {
                writer.WriteLine(instance);
                return TaskEx.CompletedTask;
            }

            readonly string instance;

            readonly TextWriter writer;

            public class Registration : RegisterStep
            {
                public Registration(TextWriter writer) : base("Terminator", typeof(Terminator), "Terminator", b => new Terminator("terminator", writer))
                {
                }
            }
        }

        class FakePipelineCache : IPipelineCache
        {
            public IPipeline<TContext> Pipeline<TContext>()
                where TContext : IBehaviorContext

            {
                return (IPipeline<TContext>)new FakeBatchPipeline();
            }
        }

        class FakeBatchPipeline : IPipeline<IBatchDispatchContext>
        {
            public Task Invoke(IBatchDispatchContext context)
            {
                return TaskEx.CompletedTask;
            }
        }
    }

    static class LambdaExpressionPrettyPrint
    {
        public static string PrettyPrint(this LambdaExpression expression)
        {
            var sb = new StringBuilder();
            var splitted = expression.ToString().Split(',');
            for (var i = 0; i < splitted.Length; i++)
            {
                sb.AppendLine($"{new string(' ', i*4)}{splitted[i].TrimStart()},");
            }
            return sb.ToString();
        }
    }
}