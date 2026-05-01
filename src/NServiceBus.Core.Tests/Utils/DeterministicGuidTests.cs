#nullable enable

namespace NServiceBus.Core.Tests.Utils;

using System;
using System.IO.Hashing;
using NServiceBus.Utils;
using NUnit.Framework;

[TestFixture]
public class DeterministicGuidTests
{
    [Test]
    public void Should_return_same_guid_for_same_string()
    {
        var first = DeterministicGuid.Create("endpoint-name");

        var second = DeterministicGuid.Create("endpoint-name");

        Assert.That(second, Is.EqualTo(first));
    }

    [Test]
    public void Should_return_different_guid_for_different_strings()
    {
        var first = DeterministicGuid.Create("endpoint-a");

        var second = DeterministicGuid.Create("endpoint-b");

        Assert.That(second, Is.Not.EqualTo(first));
    }

    [Test]
    public void Should_return_same_guid_for_string_and_char_span()
    {
        const string value = "endpoint-name";

        var fromString = DeterministicGuid.Create(value);

        var fromSpan = DeterministicGuid.Create(value.AsSpan());

        Assert.That(fromSpan, Is.EqualTo(fromString));
    }

    [Test]
    public void Should_return_same_guid_for_utf8_bytes_and_string()
    {
        const string value = "endpoint-name";

        var bytes = System.Text.Encoding.UTF8.GetBytes(value);

        var fromString = DeterministicGuid.Create(value);

        var fromBytes = DeterministicGuid.Create(bytes);

        Assert.That(fromBytes, Is.EqualTo(fromString));
    }

    [Test]
    public void Should_create_version_8_guid()
    {
        var guid = DeterministicGuid.Create("endpoint-name");

        Assert.That(guid.Version, Is.EqualTo(8));
    }

    [Test]
    public void Should_create_rfc_variant_guid()

    {
        var guid = DeterministicGuid.Create("endpoint-name");

        var bytes = guid.ToByteArray(bigEndian: true);

        Assert.That(bytes[8] & 0xC0, Is.EqualTo(0x80));
    }

    [Test]
    public void Should_frame_multiple_values_to_avoid_concatenation_ambiguity()
    {
        var first = DeterministicGuid.Create("ab", "c");

        var second = DeterministicGuid.Create("a", "bc");

        Assert.That(second, Is.Not.EqualTo(first));
    }

    [Test]
    public void Should_not_equal_single_concatenated_value()
    {
        var framed = DeterministicGuid.Create("ab", "c");

        var concatenated = DeterministicGuid.Create("abc");

        Assert.That(framed, Is.Not.EqualTo(concatenated));
    }

    [Test]
    public void Should_preserve_empty_values_in_framing()
    {
        var first = DeterministicGuid.Create("", "abc");

        var second = DeterministicGuid.Create("abc");

        Assert.That(first, Is.Not.EqualTo(second));
    }

    [Test]
    public void Should_handle_unicode_values()
    {
        var first = DeterministicGuid.Create("Grüezi", "🚀");

        var second = DeterministicGuid.Create("Grüezi", "🚀");

        Assert.That(second, Is.EqualTo(first));
    }

    [Test]
    public void Should_handle_large_input()
    {
        var value = new string('x', 10_000);

        var first = DeterministicGuid.Create(value);

        var second = DeterministicGuid.Create(value);

        Assert.That(second, Is.EqualTo(first));
    }

    [Test]
    public void Should_handle_empty_string()
    {
        var first = DeterministicGuid.Create("");

        var second = DeterministicGuid.Create("");

        using (Assert.EnterMultipleScope())
        {
            Assert.That(second, Is.EqualTo(first));
            Assert.That(first, Is.Not.EqualTo(Guid.Empty));
        }
    }

    [Test]
    public void Should_handle_empty_values_collection()
    {
        var first = DeterministicGuid.Create();

        var second = DeterministicGuid.Create();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(second, Is.EqualTo(first));
            Assert.That(first.Version, Is.EqualTo(8));
        }
    }

    [Test]
    public void Should_return_correct_hardcoded_guid_for_empty_values()
    {
        var result = DeterministicGuid.Create();

        // Verify the hardcoded constant matches what XxHash128(seed: 0) over empty input produces.
        Span<byte> hash = stackalloc byte[16];
        _ = new XxHash128().GetCurrentHash(hash);
        hash[6] = (byte)((hash[6] & 0x0F) | 0x80);
        hash[8] = (byte)((hash[8] & 0x3F) | 0x80);
        var expected = new Guid(hash, bigEndian: true);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result, Is.EqualTo(expected));
            Assert.That(result.Version, Is.EqualTo(8));
        }
    }

    [Test]
    public void Should_throw_for_null_string()
    {
        string? value = null;

        Assert.Throws<ArgumentNullException>(() => DeterministicGuid.Create(value!));
    }

    [Test]
    public void Should_throw_for_null_value_in_params()
    {
        string? value = null;

        Assert.Throws<ArgumentNullException>(() => DeterministicGuid.Create("a", value!, "b"));
    }
}