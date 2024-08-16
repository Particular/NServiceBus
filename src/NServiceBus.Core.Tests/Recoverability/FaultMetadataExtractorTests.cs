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

        Assert.AreEqual("System.AggregateException", metadata["NServiceBus.ExceptionInfo.ExceptionType"]);
        Assert.AreEqual(exception.ToString(), metadata["NServiceBus.ExceptionInfo.StackTrace"]);
        Assert.That(metadata.ContainsKey("NServiceBus.TimeOfFailure"), Is.True);

        Assert.AreEqual("System.Exception", metadata["NServiceBus.ExceptionInfo.InnerExceptionType"]);
        Assert.AreEqual("A fake help link", metadata["NServiceBus.ExceptionInfo.HelpLink"]);
        Assert.AreEqual("NServiceBus.Core.Tests", metadata["NServiceBus.ExceptionInfo.Source"]);
        Assert.AreEqual("my-address", metadata[FaultsHeaderKeys.FailedQ]);
    }

    [Test]
    public void ExceptionMessageIsTruncated()
    {
        var exception = new Exception(new string('x', (int)Math.Pow(2, 15)));
        var extractor = new FaultMetadataExtractor([], _ => { });

        var metadata = extractor.Extract(CreateErrorContext(exception));

        Assert.AreEqual((int)Math.Pow(2, 14), metadata["NServiceBus.ExceptionInfo.Message"].Length);
    }

    [Test]
    public void ShouldApplyStaticMetadata()
    {
        var extractor = new FaultMetadataExtractor(new Dictionary<string, string> { { "static-key", "some value" } }, _ => { });

        var metadata = extractor.Extract(CreateErrorContext());

        Assert.AreEqual("some value", metadata["static-key"]);
    }

    [Test]
    public void ShouldApplyCustomizations()
    {
        var extractor = new FaultMetadataExtractor(new Dictionary<string, string> { { "static-key", "some value" } }, m =>
        {
            m["static-key"] = "some other value";
        });

        var metadata = extractor.Extract(CreateErrorContext());

        Assert.AreEqual("some other value", metadata["static-key"]);
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