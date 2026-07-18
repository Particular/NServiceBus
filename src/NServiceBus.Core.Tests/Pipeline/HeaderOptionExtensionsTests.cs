namespace NServiceBus.Core.Tests.Pipeline;

using NUnit.Framework;

[TestFixture]
public class HeaderOptionExtensionsTests
{
    [Test]
    public void GetHeaders_Should_Return_Configured_Headers()
    {
        var options = new SendOptions();
        options.SetHeader("custom header key 1", "custom header value 1");
        options.SetHeader("custom header key 2", "custom header value 2");

        var result = options.GetHeaders();

        Assert.That(result.Count, Is.EqualTo(2));
        Assert.That(result.Values, Has.Member("custom header value 1"));
        Assert.That(result.Values, Has.Member("custom header value 2"));
    }

    [Test]
    public void SetHeader_allows_null_value()
    {
        var options = new SendOptions();

        Assert.DoesNotThrow(() => options.SetHeader("MyHeader", null));

        var headers = options.GetHeaders();
        Assert.That(headers["MyHeader"], Is.Null);
    }
}