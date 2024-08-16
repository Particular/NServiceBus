namespace NServiceBus.Core.Tests.Recoverability;

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using NServiceBus.Extensibility;
using NServiceBus.Faults;
using NServiceBus.Transport;
using NUnit.Framework;

[TestFixture]
public class FaultMetadataExtractorTests
{
    [Test]
    public void VerifyExceptionHeadersAreSet()
    {
        var exception = GetAnException();

        var extractor = new FaultMetadataExtractor([], _ => { });

        var metadata = extractor.Extract(CreateErrorContext(exception));

        Assert.That(metadata["NServiceBus.ExceptionInfo.ExceptionType"], Is.EqualTo("System.AggregateException"));
        Assert.That(metadata["NServiceBus.ExceptionInfo.StackTrace"], Is.EqualTo(exception.ToString()));
        Assert.That(metadata.ContainsKey("NServiceBus.TimeOfFailure"), Is.True);

        Assert.That(metadata["NServiceBus.ExceptionInfo.InnerExceptionType"], Is.EqualTo("System.Exception"));
        Assert.That(metadata["NServiceBus.ExceptionInfo.HelpLink"], Is.EqualTo("A fake help link"));
        Assert.That(metadata["NServiceBus.ExceptionInfo.Source"], Is.EqualTo("NServiceBus.Core.Tests"));
        Assert.That(metadata[FaultsHeaderKeys.FailedQ], Is.EqualTo("my-address"));
    }

    [Test]
    public void ExceptionMessageIsTruncated()
    {
        var exception = new Exception(new string('x', (int)Math.Pow(2, 15)));
        var extractor = new FaultMetadataExtractor([], _ => { });

        var metadata = extractor.Extract(CreateErrorContext(exception));

        Assert.That(metadata["NServiceBus.ExceptionInfo.Message"].Length, Is.EqualTo((int)Math.Pow(2, 14)));
    }

    [Test]
    public void ShouldApplyStaticMetadata()
    {
        var extractor = new FaultMetadataExtractor(new Dictionary<string, string> { { "static-key", "some value" } }, _ => { });

        var metadata = extractor.Extract(CreateErrorContext());

        Assert.That(metadata["static-key"], Is.EqualTo("some value"));
    }

    [Test]
    public void ShouldApplyCustomizations()
    {
        var extractor = new FaultMetadataExtractor(new Dictionary<string, string> { { "static-key", "some value" } }, m =>
        {
            m["static-key"] = "some other value";
        });

        var metadata = extractor.Extract(CreateErrorContext());

        Assert.That(metadata["static-key"], Is.EqualTo("some other value"));
    }

    static ErrorContext CreateErrorContext(Exception exception = null) => new(exception ?? GetAnException(), [], "some-id", Array.Empty<byte>(), new TransportTransaction(), 0, "my-address", new ContextBag());

    static Exception GetAnException()
    {
        try
        {
            MethodThatThrows1();
        }
        catch (Exception e)
        {
            return e;
        }
        return null;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void MethodThatThrows1()
    {
        try
        {
            MethodThatThrows2();
        }
        catch (Exception exception)
        {
            throw new AggregateException("My Exception", exception) { HelpLink = "A fake help link" };
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static void MethodThatThrows2()
    {
        throw new Exception("My Inner Exception");
    }
}