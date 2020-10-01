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
    using Extensibility;
    using Microsoft.Extensions.DependencyInjection;
    using NServiceBus.Pipeline;
    using NUnit.Framework;
    using Particular.Approvals;
    using Testing;

    [TestFixture]
    public class PipelineTests
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
            pipelineModifications.Additions.Add(new Stage2.Registration(stringWriter));
            pipelineModifications.Additions.Add(new Terminator.Registration(stringWriter));

            var pipeline = new Pipeline<ITransportReceiveContext>(new ServiceCollection().BuildServiceProvider(), pipelineModifications);

            var context = new TestableTransportReceiveContext();
            context.Extensions.Set<IPipelineCache>(new FakePipelineCache());

            await pipeline.Invoke(context);

            Approver.Verify(stringWriter.ToString());
        }

        [Test]
        public async Task ShouldNotCacheContext()
        {
            var stringWriter = new StringWriter();

            var pipelineModifications = new PipelineModifications();
            pipelineModifications.Additions.Add(new Behavior1.Registration(stringWriter));
            pipelineModifications.Additions.Add(new Stage1.Registration(stringWriter));
            pipelineModifications.Additions.Add(new Behavior2.Registration(stringWriter));
            pipelineModifications.Additions.Add(new StageFork.Registration(stringWriter));
            pipelineModifications.Additions.Add(new Stage2.Registration(stringWriter));
            pipelineModifications.Additions.Add(new Terminator.Registration(stringWriter));

            var pipeline = new Pipeline<ITransportReceiveContext>(new ServiceCollection().BuildServiceProvider(), pipelineModifications);

            stringWriter.WriteLine("Run 1");

            var context = new TestableTransportReceiveContext();
            context.Extensions.Set<IPipelineCache>(new FakePipelineCache());
            context.Extensions.Set(ExtendableExtensions.RunSpecificKey, 1);

            await pipeline.Invoke(context);

            stringWriter.WriteLine("Run 2");

            context = new TestableTransportReceiveContext();
            context.Extensions.Set<IPipelineCache>(new FakePipelineCache());
            context.Extensions.Set(ExtendableExtensions.RunSpecificKey, 2);

            await pipeline.Invoke(context);

            Approver.Verify(stringWriter.ToString());
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

            var expressions = new List<Expression>();
            behaviors.CreatePipelineExecutionExpression(expressions);

            Approver.Verify(expressions.PrettyPrint());
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

            var pipeline = new Pipeline<ITransportReceiveContext>(new ServiceCollection().BuildServiceProvider(), pipelineModifications);

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

        class StageFork : IStageForkConnector<ITransportReceiveContext, IIncomingPhysicalMessageContext, IBatchDispatchContext>
        {
            public StageFork(string instance, TextWriter writer)
            {
                this.instance = instance;
                this.writer = writer;
            }

            public async Task Invoke(ITransportReceiveContext context, Func<IIncomingPhysicalMessageContext, Task> next)
            {
                context.PrintInstanceWithRunSpecificIfPossible(instance, writer);

                var physicalMessageContext = new TestableIncomingPhysicalMessageContext();
                physicalMessageContext.Extensions.Merge(context.Extensions);

                await next(physicalMessageContext).ConfigureAwait(false);

                var dispatchContext = new TestableBatchDispatchContext();
                dispatchContext.Extensions.Merge(context.Extensions);

                await this.Fork(dispatchContext).ConfigureAwait(false);
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



        class Behavior1 : IBehavior<IIncomingPhysicalMessageContext, IIncomingPhysicalMessageContext>
        {
            public Behavior1(string instance, TextWriter writer)
            {
                this.instance = instance;
                this.writer = writer;
            }

            public Task Invoke(IIncomingPhysicalMessageContext context, Func<IIncomingPhysicalMessageContext, Task> next)
            {
                context.PrintInstanceWithRunSpecificIfPossible(instance, writer);
                return next(context);
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
                context.PrintInstanceWithRunSpecificIfPossible(instance, writer);

                var logicalMessageContext = new TestableIncomingLogicalMessageContext();
                logicalMessageContext.Extensions.Merge(context.Extensions);

                return stage(logicalMessageContext);
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

        class Behavior2 : IBehavior<IIncomingLogicalMessageContext, IIncomingLogicalMessageContext>
        {
            public Behavior2(string instance, TextWriter writer)
            {
                this.instance = instance;
                this.writer = writer;
            }

            public Task Invoke(IIncomingLogicalMessageContext context, Func<IIncomingLogicalMessageContext, Task> next)
            {
                writer.WriteLine(instance);
                return next(context);
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
                context.PrintInstanceWithRunSpecificIfPossible(instance, writer);

                var dispatchContext = new TestableDispatchContext();
                dispatchContext.Extensions.Merge(context.Extensions);

                return stage(dispatchContext);
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
                context.PrintInstanceWithRunSpecificIfPossible(instance, writer);
                return Task.CompletedTask;
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
                return Task.CompletedTask;
            }
        }
    }

    static class LambdaExpressionPrettyPrint
    {
        public static string PrettyPrint(this List<Expression> expression)
        {
            expression.Reverse();
            var sb = new StringBuilder();
            for (var i = 0; i < expression.Count; i++)
            {
                sb.AppendLine($"{new string(' ', i * 4)}{expression[i].ToString().TrimStart()},");
            }
            return sb.ToString();
        }
    }

    static class ExtendableExtensions
    {
        public const string RunSpecificKey = "RunSpecific";

        public static void PrintInstanceWithRunSpecificIfPossible(this IExtendable context, string instance, TextWriter writer)
        {
            if (context.Extensions.TryGet(RunSpecificKey, out int runSpecific))
            {
                writer.WriteLine($"{instance}: {runSpecific}");
            }
            else
            {
                writer.WriteLine(instance);
            }
        }
    }
}