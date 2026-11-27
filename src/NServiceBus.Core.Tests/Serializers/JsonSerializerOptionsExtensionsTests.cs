namespace NServiceBus.Core.Tests.Serializers;

using System.Text.Json;
using System.Text.Json.Serialization;
using NUnit.Framework;

[TestFixture]
public partial class JsonSerializerOptionsExtensionsTests
{
    [Test]
    public void Should_reuse_metadata_from_a_type_info_resolver_once_serialization_made_the_options_read_only()
    {
        var options = new JsonSerializerOptions { TypeInfoResolver = ExtensionsTestsJsonContext.Default };
        JsonSerializer.Serialize(new SomeMessage(), options.ResolveTypeInfo(typeof(SomeMessage))!);

        var first = options.ResolveTypeInfo(typeof(SomeMessage));
        var second = options.ResolveTypeInfo(typeof(SomeMessage));

        Assert.That(second, Is.Not.Null.And.SameAs(first));
    }

    [Test]
    public void Should_reuse_reflection_metadata_once_serialization_made_the_options_read_only()
    {
        var options = CreateWritableOptions();
        Assert.That(options.ResolveTypeInfo(typeof(SomeMessage)), Is.Null, "Options without a resolver use reflection serialization");
        JsonSerializer.Serialize(new SomeMessage(), options);

        var first = options.ResolveTypeInfo(typeof(SomeMessage));
        var second = options.ResolveTypeInfo(typeof(SomeMessage));

        Assert.That(second, Is.Not.Null.And.SameAs(first));
    }

    [Test]
    public void Should_leave_types_the_resolver_omits_to_reflection_serialization() =>
        Assert.That(UnrelatedJsonContext.Default.Options.ResolveTypeInfo(typeof(SomeMessage)), Is.Null);

    static JsonSerializerOptions CreateWritableOptions() => new();

    public class SomeMessage
    {
        public int Value { get; set; }
    }

    [JsonSerializable(typeof(SomeMessage))]
    partial class ExtensionsTestsJsonContext : JsonSerializerContext;

    [JsonSerializable(typeof(int))]
    partial class UnrelatedJsonContext : JsonSerializerContext;
}
