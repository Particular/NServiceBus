namespace NServiceBus.Core.Tests.Pipeline.Incoming;

using System;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using NUnit.Framework;

[TestFixture]
public class IncomingPipelineMetricsTagBagConventionTests
{
    // Every tag applied to a metric must flow through IncomingPipelineMetricTags.Add first, so that a consumer can
    // always add, remove, or override it there. A direct `tags.Add(new ...)`/`meterTags.Add(new ...)` on a local
    // TagList bypasses that and can never be customized.
    [Test]
    public void Should_only_add_tags_through_IncomingPipelineMetricTags()
    {
        var sourcePath = GetSourcePath();
        var lines = File.ReadAllLines(sourcePath);
        var bypassPattern = new Regex(@"\b(tags|meterTags)\.Add\(new\b");

        var violations = lines
            .Select((line, index) => (line, number: index + 1))
            .Where(x => bypassPattern.IsMatch(x.line) && !x.line.Contains(AllowedBypassMarker))
            .Select(x => $"{sourcePath}:{x.number}: {x.line.Trim()}")
            .ToList();

        Assert.That(violations, Is.Empty,
            $"Found tags added directly to a TagList instead of through {nameof(IncomingPipelineMetricTags)}.{nameof(IncomingPipelineMetricTags.Add)}. " +
            "That prevents consumers from adding, removing, or overriding the tag. " +
            $"Route the value through {nameof(IncomingPipelineMetricTags)} first, or mark the line with " +
            $"'{AllowedBypassMarker}' if there's genuinely no {nameof(IncomingPipelineMetricTags)} available at that point." +
            Environment.NewLine + string.Join(Environment.NewLine, violations));
    }

    const string AllowedBypassMarker = "tag-bag-bypass";

    static string GetSourcePath([CallerFilePath] string thisFilePath = "") =>
        Path.GetFullPath(Path.Combine(Path.GetDirectoryName(thisFilePath)!, "..", "..", "NServiceBus.Core", "Pipeline", "Incoming", "IncomingPipelineMetrics.cs"));
}
