namespace NServiceBus.Core.Tests.Pipeline;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.ExceptionServices;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Extensibility;
using Microsoft.Extensions.DependencyInjection;
using NServiceBus.Pipeline;
using NUnit.Framework;
using Particular.Approvals;
using Testing;

[TestFixture]
public partial class PipelineTests
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

        var coordinator = new StepRegistrationsCoordinator(pipelineModifications.Additions, pipelineModifications.Replacements, pipelineModifications.AdditionsOrReplacements);
        var steps = coordinator.BuildPipelineFor<ITransportReceiveContext>();

        Approver.Verify(PipelineStepDiagnostics.PrettyPrint(steps));
    }

    [Test]
    public async Task ShouldCacheExecutionFunc()
    {
        var pipelineModifications = new PipelineModifications();
        pipelineModifications.AddAddition(new Behavior1.Registration(TextWriter.Null));
        pipelineModifications.AddAddition(new Stage1.Registration(TextWriter.Null));
        pipelineModifications.AddAddition(new Behavior2.Registration(TextWriter.Null));
        pipelineModifications.AddAddition(new StageFork.Registration(TextWriter.Null));

        await using var serviceProvider = new ServiceCollection().BuildServiceProvider();
        var context = new TestableTransportReceiveContext();
        context.Extensions.Set<IPipelineCache>(new FakePipelineCache());

        var stopwatch = Stopwatch.StartNew();
        var pipeline = new Pipeline<ITransportReceiveContext>(serviceProvider, pipelineModifications);
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

        Assert.That(average, Is.LessThan(firstRunTicks));
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

    [Test]
    public async Task ShouldReplayNextDelegateMultipleTimes()
    {
        var pipelineModifications = new PipelineModifications();
        pipelineModifications.AddAddition(new StageFork.Registration(TextWriter.Null));
        pipelineModifications.AddAddition(new Stage1.Registration(TextWriter.Null));
        pipelineModifications.AddAddition(new Stage2.Registration(TextWriter.Null));
        pipelineModifications.AddAddition(new InvokeReplayBehavior.Registration());
        pipelineModifications.AddAddition(new CountingTerminator.Registration());

        await using var serviceProvider = new ServiceCollection().BuildServiceProvider();
        var pipeline = new Pipeline<ITransportReceiveContext>(serviceProvider, pipelineModifications);

        var context = new TestableTransportReceiveContext();
        context.Extensions.Set<IPipelineCache>(new FakePipelineCache());

        await pipeline.Invoke(context);

        Assert.That(context.Extensions.Get<int>(ReplayCountKey), Is.EqualTo(2));
    }

    [Test]
    public async Task Should_provide_clean_stack_trace_when_exception_thrown()
    {
        var pipelineModifications = new PipelineModifications();
        pipelineModifications.AddAddition(new Behavior1.Registration(TextWriter.Null));
        pipelineModifications.AddAddition(new Stage1.Registration(TextWriter.Null));
        pipelineModifications.AddAddition(new Behavior2.Registration(TextWriter.Null));
        pipelineModifications.AddAddition(new StageFork.Registration(TextWriter.Null));
        pipelineModifications.AddAddition(new Stage2.Registration(TextWriter.Null));
        pipelineModifications.AddAddition(new ThrowingTerminator.Registration());

        await using var serviceProvider = new ServiceCollection().BuildServiceProvider();
        var pipeline = new Pipeline<ITransportReceiveContext>(serviceProvider, pipelineModifications);

        var context = new TestableTransportReceiveContext();
        context.Extensions.Set<IPipelineCache>(new FakePipelineCache());

        ExceptionDispatchInfo info = null;
        try
        {
            await pipeline.Invoke(context);
        }
#pragma warning disable PS0019
        catch (Exception ex)
#pragma warning restore PS0019
        {
            info = ExceptionDispatchInfo.Capture(ex);
        }

        var stackTrace = info?.SourceException.StackTrace ?? "";

        using (Assert.EnterMultipleScope())
        {
            Assert.That(stackTrace, Does.Not.Contain("PipelineExecution"));
            Assert.That(stackTrace, Does.Not.Contain("PipelineRunner"));
            Assert.That(stackTrace, Does.Not.Contain("PipelineNode"));
        }

        Approver.Verify(stackTrace, scrubber: ScrubFileInfoFromStackTrace);
    }

    static string ScrubFileInfoFromStackTrace(string value)
    {
        var scrubbedPaths = StackTracePathRegex().Replace(value, static match =>
        {
            var path = match.Groups["path"].Value;
            var parts = path.Split(['/', '\\'], StringSplitOptions.RemoveEmptyEntries);

            return parts.Length switch
            {
                >= 2 => $"{parts[^2]}/{parts[^1]}",
                1 => parts[0],
                _ => string.Empty
            };
        });

        return StackTraceLineInfoRegex().Replace(scrubbedPaths, string.Empty);
    }

    [GeneratedRegex(@"(?<path>(?:[A-Za-z]:)?(?:[/\\][^:/\\\r\n]+)+\.cs)")]
    private static partial Regex StackTracePathRegex();

    [GeneratedRegex(@"(?<=\.cs):\p{L}+\s+\d+")]
    private static partial Regex StackTraceLineInfoRegex();

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

    sealed class ThrowingTerminator : PipelineTerminator<IInvokeHandlerContext>
    {
        protected override Task Terminate(IInvokeHandlerContext context)
        {
            throw new Exception("Test exception to verify stack trace");
        }

        public class Registration() : RegisterStep("ThrowingTerminator", typeof(ThrowingTerminator), "ThrowingTerminator", _ => new ThrowingTerminator());
    }

    sealed class InvokeReplayBehavior : IBehavior<IInvokeHandlerContext, IInvokeHandlerContext>
    {
        public async Task Invoke(IInvokeHandlerContext context, Func<IInvokeHandlerContext, Task> next)
        {
            await next(context).ConfigureAwait(false);
            await next(context).ConfigureAwait(false);
        }

        public class Registration() : RegisterStep("InvokeReplayBehavior", typeof(InvokeReplayBehavior), "InvokeReplayBehavior", _ => new InvokeReplayBehavior());
    }

    sealed class CountingTerminator : PipelineTerminator<IInvokeHandlerContext>
    {
        protected override Task Terminate(IInvokeHandlerContext context)
        {
            context.Extensions.TryGet(ReplayCountKey, out int count);
            context.Extensions.Set(ReplayCountKey, count + 1);
            return Task.CompletedTask;
        }

        public class Registration() : RegisterStep("CountingTerminator", typeof(CountingTerminator), "CountingTerminator", _ => new CountingTerminator());
    }

    const string ReplayCountKey = nameof(ReplayCountKey);

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
