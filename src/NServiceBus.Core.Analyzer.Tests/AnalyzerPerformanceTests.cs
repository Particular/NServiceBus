namespace NServiceBus.Core.Analyzer.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Threading.Tasks;
    using Microsoft.CodeAnalysis.Diagnostics;
    using Microsoft.CodeAnalysis.MSBuild;
    using NUnit.Framework;

    public class AnalyzerPerformanceTests
    {
        [Test]
        public async Task MeasureAnalyzerPerformance()
        {
            var ws = MSBuildWorkspace.Create(new Dictionary<string, string> { ["TargetFramework"] = "net452" });

//            var projectFilePath = Path.Combine(TestContext.CurrentContext.TestDirectory, "../../../../NServiceBus.Core/NServiceBus.Core.csproj");
            var projectFilePath = Path.Combine(TestContext.CurrentContext.TestDirectory, "../../../../NServiceBus.AcceptanceTests/NServiceBus.AcceptanceTests.csproj");
            var project = await ws.OpenProjectAsync(projectFilePath);
            Assert.Greater(project.DocumentIds.Count, 0); // ensure project is properly loaded.

            var compilation = await project.GetCompilationAsync();
            var diagnosticAnalyzers = ImmutableArray.Create<DiagnosticAnalyzer>(new AwaitOrCaptureTasksAnalyzer());

            var warmup = compilation.WithAnalyzers(diagnosticAnalyzers);
            await warmup.GetAnalyzerDiagnosticsAsync();

            var stopwatch = new Stopwatch();
            var elapsed = new TimeSpan[100];
            stopwatch.Start();
            for (var i = 0; i < elapsed.Length; i++)
            {
                var result = compilation.WithAnalyzers(diagnosticAnalyzers);
                stopwatch.Restart();
                var analyzerResults = await result.GetAnalyzerDiagnosticsAsync();
                stopwatch.Stop();
                Assert.AreEqual(0, analyzerResults.Length);
                TestContext.WriteLine(elapsed[i] = stopwatch.Elapsed);
            }

            Console.WriteLine($"Average: {TimeSpan.FromTicks((long)elapsed.Average(x => x.Ticks)).ToString()}");
            Console.WriteLine($"50% Percentile: {GetPercentile(0.5f, elapsed)}");
            Console.WriteLine($"80% Percentile: {GetPercentile(0.8f, elapsed)}");
            Console.WriteLine($"95% Percentile: {GetPercentile(0.95f, elapsed)}");
        }

        static TimeSpan GetPercentile(float percentile, TimeSpan[] values)
        {
            var ordered = values.OrderBy(x => x).ToArray();

            var lower = Math.Floor((values.Length + 1) * percentile);
            var upper = Math.Ceiling((values.Length + 1) * percentile);

            var ticks = (ordered[(int)lower].Ticks + ordered[(int)upper].Ticks) / 2;
            return TimeSpan.FromTicks(ticks);
        }
    }
}