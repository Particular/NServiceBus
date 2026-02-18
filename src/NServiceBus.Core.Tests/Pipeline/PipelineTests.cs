namespace NServiceBus.Core.Tests.Pipeline;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
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
        pipelineModifications.AddAddition(new Behavior1.Registration(stringWriter));
        pipelineModifications.AddAddition(new Stage1.Registration(stringWriter));
        pipelineModifications.AddAddition(new Behavior2.Registration(stringWriter));
        pipelineModifications.AddAddition(new StageFork.Registration(stringWriter));
        pipelineModifications.AddAddition(new Stage2.Registration(stringWriter));
        pipelineModifications.AddAddition(new Terminator.Registration(stringWriter));

        await using var serviceProvider = new ServiceCollection().BuildServiceProvider();
        var pipeline = new Pipeline<ITransportReceiveContext>(serviceProvider, pipelineModifications);

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
        pipelineModifications.AddAddition(new Behavior1.Registration(stringWriter));
        pipelineModifications.AddAddition(new Stage1.Registration(stringWriter));
        pipelineModifications.AddAddition(new Behavior2.Registration(stringWriter));
        pipelineModifications.AddAddition(new StageFork.Registration(stringWriter));
        pipelineModifications.AddAddition(new Stage2.Registration(stringWriter));
        pipelineModifications.AddAddition(new Terminator.Registration(stringWriter));

        await using var serviceProvider = new ServiceCollection().BuildServiceProvider();
        var pipeline = new Pipeline<ITransportReceiveContext>(serviceProvider, pipelineModifications);

        await stringWriter.WriteLineAsync("Run 1");

        var context = new TestableTransportReceiveContext();
        context.Extensions.Set<IPipelineCache>(new FakePipelineCache());
        context.Extensions.Set(ExtendableExtensions.RunSpecificKey, 1);

        await pipeline.Invoke(context);

        await stringWriter.WriteLineAsync("Run 2");

        context = new TestableTransportReceiveContext();
        context.Extensions.Set<IPipelineCache>(new FakePipelineCache());
        context.Extensions.Set(ExtendableExtensions.RunSpecificKey, 2);

        await pipeline.Invoke(context);

        Approver.Verify(stringWriter.ToString());
    }

    [Test]
    public void ShouldCreateCachedExecutionPlan()
    {
        var pipelineModifications = new PipelineModifications();
        pipelineModifications.AddAddition(new Behavior1.Registration(TextWriter.Null));
        pipelineModifications.AddAddition(new Stage1.Registration(TextWriter.Null));
        pipelineModifications.AddAddition(new Behavior2.Registration(TextWriter.Null));
        pipelineModifications.AddAddition(new StageFork.Registration(TextWriter.Null));
        pipelineModifications.AddAddition(new Stage2.Registration(TextWriter.Null));
        pipelineModifications.AddAddition(new Terminator.Registration(TextWriter.Null));

        using var serviceProvider = new ServiceCollection().BuildServiceProvider();
        var pipeline = new Pipeline<ITransportReceiveContext>(serviceProvider, pipelineModifications);

        var parts = GetPipelinePartsField(pipeline);

        Approver.Verify(PipelinePartDiagnostics.PrettyPrint(parts));
    }

    static PipelinePart[] GetPipelinePartsField(Pipeline<ITransportReceiveContext> pipeline)
    {
        var field = typeof(Pipeline<ITransportReceiveContext>).GetField("parts", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        return (PipelinePart[])field!.GetValue(pipeline)!;
    }

    [Test]
    public async Task ShouldCacheExecutionFunc()
    {
        var stringWriter = new StringWriter();

        var pipelineModifications = new PipelineModifications();
        pipelineModifications.AddAddition(new Behavior1.Registration(stringWriter));
        pipelineModifications.AddAddition(new Stage1.Registration(stringWriter));
        pipelineModifications.AddAddition(new Behavior2.Registration(stringWriter));
        pipelineModifications.AddAddition(new StageFork.Registration(stringWriter));

        await using var serviceProvider = new ServiceCollection().BuildServiceProvider();
        var pipeline = new Pipeline<ITransportReceiveContext>(serviceProvider, pipelineModifications);

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

    [Test]
    public async Task ShouldAllowExecutingPartsOfThePipelineMultipleTimes()
    {
        var stringWriter = new StringWriter();

        var pipelineModifications = new PipelineModifications();
        pipelineModifications.AddAddition(new Behavior1.Registration(stringWriter));
        pipelineModifications.AddAddition(new Stage1.Registration(stringWriter));
        pipelineModifications.AddAddition(new Behavior2.Registration(stringWriter));
        pipelineModifications.AddAddition(new StageFork.Registration(stringWriter));
        pipelineModifications.AddAddition(new Stage2.Registration(stringWriter));
        pipelineModifications.AddAddition(new Terminator.Registration(stringWriter));
        pipelineModifications.AddAddition(new BehaviorWithRetry.Registration(stringWriter));

        await using var serviceProvider = new ServiceCollection().BuildServiceProvider();
        var pipeline = new Pipeline<ITransportReceiveContext>(serviceProvider, pipelineModifications);

        var context = new TestableTransportReceiveContext();
        context.Extensions.Set<IPipelineCache>(new FakePipelineCache());

        await pipeline.Invoke(context);

        Approver.Verify(stringWriter.ToString());
    }

    class StageFork(string instance, TextWriter writer) : IStageForkConnector<ITransportReceiveContext, IIncomingPhysicalMessageContext, IBatchDispatchContext>
    {
        public async Task Invoke(ITransportReceiveContext context, Func<IIncomingPhysicalMessageContext, Task> next)
        {
            context.PrintInstanceWithRunSpecificIfPossible(instance, writer);

            var physicalMessageContext = new TestableIncomingPhysicalMessageContext { Extensions = context.Extensions };

            await next(physicalMessageContext).ConfigureAwait(false);

            var dispatchContext = new TestableBatchDispatchContext { Extensions = context.Extensions };

            await this.Fork(dispatchContext).ConfigureAwait(false);
        }

        public class Registration(TextWriter writer) : RegisterStep("StageFork", typeof(StageFork), "StageFork", b => new StageFork("stagefork1", writer));
    }

    class BehaviorWithRetry(string instance, TextWriter writer) : IBehavior<IIncomingPhysicalMessageContext, IIncomingPhysicalMessageContext>
    {
        public async Task Invoke(IIncomingPhysicalMessageContext context, Func<IIncomingPhysicalMessageContext, Task> next)
        {
            for (int i = 1; i < 4; i++)
            {
                context.PrintInstanceWithRunSpecificIfPossible(instance + i, writer);
                await next(context);
            }
        }

        public class Registration(TextWriter writer) : RegisterStep("BehaviorWithRetry", typeof(BehaviorWithRetry), "BehaviorWithRetry", b => new BehaviorWithRetry("BehaviorWithRetry", writer));
    }

    class Behavior1(string instance, TextWriter writer) : IBehavior<IIncomingPhysicalMessageContext, IIncomingPhysicalMessageContext>
    {
        public Task Invoke(IIncomingPhysicalMessageContext context, Func<IIncomingPhysicalMessageContext, Task> next)
        {
            context.PrintInstanceWithRunSpecificIfPossible(instance, writer);
            return next(context);
        }

        public class Registration(TextWriter writer) : RegisterStep("Behavior1", typeof(Behavior1), "Behavior1", b => new Behavior1("behavior1", writer));
    }

    class Stage1(string instance, TextWriter writer) : StageConnector<IIncomingPhysicalMessageContext, IIncomingLogicalMessageContext>
    {
        public override Task Invoke(IIncomingPhysicalMessageContext context, Func<IIncomingLogicalMessageContext, Task> stage)
        {
            context.PrintInstanceWithRunSpecificIfPossible(instance, writer);

            var logicalMessageContext = new TestableIncomingLogicalMessageContext { Extensions = context.Extensions };

            return stage(logicalMessageContext);
        }

        public class Registration(TextWriter writer) : RegisterStep("Stage1", typeof(Stage1), "Stage1", b => new Stage1("stage1", writer));
    }

    class Behavior2(string instance, TextWriter writer) : IBehavior<IIncomingLogicalMessageContext, IIncomingLogicalMessageContext>
    {
        public Task Invoke(IIncomingLogicalMessageContext context, Func<IIncomingLogicalMessageContext, Task> next)
        {
            writer.WriteLine(instance);
            return next(context);
        }

        public class Registration(TextWriter writer) : RegisterStep("Behavior2", typeof(Behavior2), "Behavior2", b => new Behavior2("behavior2", writer));
    }

    class Stage2(string instance, TextWriter writer) : StageConnector<IIncomingLogicalMessageContext, IInvokeHandlerContext>
    {
        public override Task Invoke(IIncomingLogicalMessageContext context, Func<IInvokeHandlerContext, Task> stage)
        {
            context.PrintInstanceWithRunSpecificIfPossible(instance, writer);

            var invokeHandlerContext = new TestableInvokeHandlerContext { Extensions = context.Extensions };

            return stage(invokeHandlerContext);
        }

        public class Registration(TextWriter writer) : RegisterStep("Stage2", typeof(Stage2), "Stage2", b => new Stage2("stage2", writer));
    }

    class Terminator(string instance, TextWriter writer) : PipelineTerminator<IInvokeHandlerContext>
    {
        protected override Task Terminate(IInvokeHandlerContext context)
        {
            context.PrintInstanceWithRunSpecificIfPossible(instance, writer);
            return Task.CompletedTask;
        }

        public class Registration(TextWriter writer) : RegisterStep("Terminator", typeof(Terminator), "Terminator", b => new Terminator("terminator", writer));
    }

    class FakePipelineCache : IPipelineCache
    {
        public IPipeline<TContext> Pipeline<TContext>()
            where TContext : IBehaviorContext =>
            (IPipeline<TContext>)new FakeBatchPipeline();
    }

    class FakeBatchPipeline : IPipeline<IBatchDispatchContext>
    {
        public Task Invoke(IBatchDispatchContext context) => Task.CompletedTask;
    }
}

static class ExtendableExtensions
{
    public const string RunSpecificKey = "RunSpecific";

    public static void PrintInstanceWithRunSpecificIfPossible(this IExtendable context, string instance, TextWriter writer)
        => writer.WriteLine(context.Extensions.TryGet(RunSpecificKey, out int runSpecific) ? $"{instance}: {runSpecific}" : instance);
}