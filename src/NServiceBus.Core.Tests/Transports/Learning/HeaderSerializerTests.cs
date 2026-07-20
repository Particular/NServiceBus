#nullable enable

namespace NServiceBus.Core.Tests.Transports.Learning;

using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using NUnit.Framework;
using Particular.Approvals;
using NServiceBus.Utils;

public class HeaderSerializerTests
{
    static readonly JsonTypeInfo<Dictionary<string, string>> SourceGenTypeInfo =
        HeaderSerializer.HeaderSerializationContext.Default.DictionaryStringString;

    [Test]
    public void Can_round_trip_headers()
    {
        var input = new Dictionary<string, string>
        {
            {
                "key 1",
                "value 1"
            },
            {
                "key 2",
                "value 2"
            }
        };
        var serialized = HeaderSerializer.Serialize(input);

        Approver.Verify(serialized);
        var deserialize = HeaderSerializer.Deserialize(serialized);
        Assert.That(deserialize, Is.EquivalentTo(input));
    }

    [Test]
    public void Can_deserialize_from_utf8_bytes()
    {
        var input = new Dictionary<string, string>
        {
            { "key 1", "value 1" },
            { "key 2", "value 2" }
        };
        var serialized = HeaderSerializer.Serialize(input);
        var bytes = Encoding.UTF8.GetBytes(serialized);

        var deserialized = HeaderSerializer.Deserialize(bytes);
        Assert.That(deserialized, Is.EquivalentTo(input));
    }

    [Test]
    public void Deserialize_from_bytes_with_pool_reuses_dictionary()
    {
        var pool = new DictionaryPool<string, string>(maxPoolSize: 4);
        var input = new Dictionary<string, string>
        {
            { "key 1", "value 1" },
            { "key 2", "value 2" }
        };
        var serialized = HeaderSerializer.Serialize(input);
        var bytes = Encoding.UTF8.GetBytes(serialized);

        var first = HeaderSerializer.Deserialize(bytes, pool);
        pool.Return(first);

        var second = HeaderSerializer.Deserialize(bytes, pool);
        Assert.That(second, Is.SameAs(first));
        Assert.That(second, Is.EquivalentTo(input));
    }

    [Test]
    public void Round_trip_empty_dictionary()
    {
        var input = new Dictionary<string, string>();
        var serialized = HeaderSerializer.Serialize(input);
        var deserialized = HeaderSerializer.Deserialize(serialized);
        Assert.That(deserialized, Is.Empty);
    }

    [Test]
    public void Round_trip_special_characters_in_values()
    {
        var input = new Dictionary<string, string>
        {
            { "quotes", "value with \"quoted\" text" },
            { "backslash", "C:\\Program Files\\" },
            { "newline", "line1\nline2\r\nline3" },
            { "tab", "a\tb" },
            { "unicode", "\u00e9\u00e8\u00ea" },
            { "emoji", "\ud83d\ude00" }
        };
        var serialized = HeaderSerializer.Serialize(input);
        var deserialized = HeaderSerializer.Deserialize(serialized);
        Assert.That(deserialized, Is.EquivalentTo(input));
    }

    [Test]
    public void Round_trip_unicode_keys_and_values()
    {
        var input = new Dictionary<string, string>
        {
            { "\u00fcnicode-key", "value-with-\u00fcmlaut" },
            { "\u4e2d\u6587", "\u65e5\u672c\u8a9e" },
            { "key \u00e0", "value \u00e9" }
        };
        var serialized = HeaderSerializer.Serialize(input);
        var deserialized = HeaderSerializer.Deserialize(serialized);
        Assert.That(deserialized, Is.EquivalentTo(input));
    }

    [Test]
    public void Manual_parser_matches_source_generated_deserializer_for_serialized_output()
    {
        // The old Deserialize used JsonSerializer.Deserialize with the source-generated
        // context (AOT-friendly). The new code uses a manual Utf8JsonReader. Verify
        // they agree on every input that Serialize can produce.
        Dictionary<string, string>[] testCases =
        [
            new() { { "a", "b" } },
            new()
            {
                { "key 1", "value 1" },
                { "key 2", "value 2" }
            },
            new()
            {
                { "special", "\"quotes\" and \\backslashes" },
                { "multiline", "line1\nline2" }
            }
        ];

        foreach (var input in testCases)
        {
            var serialized = HeaderSerializer.Serialize(input);
            var bytes = Encoding.UTF8.GetBytes(serialized);

            // Old path: source-generated AOT-friendly byte-span overload
            var oldResult = JsonSerializer.Deserialize(bytes, SourceGenTypeInfo);

            // New path: manual Utf8JsonReader parser
            var newResult = HeaderSerializer.Deserialize(serialized);

            Assert.That(newResult, Is.EquivalentTo(oldResult!),
                $"Mismatch for input with {input.Count} entries. Serialized: {serialized}");
        }
    }

    [Test]
    public void Deserialize_handles_leading_and_trailing_whitespace()
    {
        var input = new Dictionary<string, string> { { "key", "value" } };
        var serialized = HeaderSerializer.Serialize(input);
        var padded = "  \n  " + serialized + "  \n  ";

        var deserialized = HeaderSerializer.Deserialize(padded);
        Assert.That(deserialized, Is.EquivalentTo(input));
    }

    [TestCase("")]
    [TestCase("not json")]
    [TestCase("[]")]
    [TestCase("123")]
    [TestCase("\"string\"")]
    [TestCase("{ \"key\": 123 }")]
    [TestCase("{ \"key\": [1, 2] }")]
    [TestCase("{ \"key\": { \"nested\": true } }")]
    [TestCase("{ \"key\": }")]
    [TestCase("{ \"key\": \"value\"")]
    public void Deserialize_throws_for_malformed_json(string malformed) => Assert.That(() => HeaderSerializer.Deserialize(malformed), Throws.InstanceOf<JsonException>());

    [Test]
    public void Deserialize_with_pool_returns_dict_that_can_be_returned_to_pool()
    {
        var pool = new DictionaryPool<string, string>(maxPoolSize: 4);
        var input = new Dictionary<string, string>
        {
            { "key 1", "value 1" },
            { "key 2", "value 2" }
        };
        var serialized = HeaderSerializer.Serialize(input);
        var bytes = Encoding.UTF8.GetBytes(serialized);

        var dict = HeaderSerializer.Deserialize(bytes, pool);
        Assert.That(dict, Has.Count.EqualTo(2));

        // Returning and re-renting should give back the same (cleared) dictionary.
        pool.Return(dict);
        var reused = pool.Rent();
        Assert.That(reused, Is.SameAs(dict));
        Assert.That(reused, Is.Empty);
    }

    [Test]
    public void Deserialize_from_bytes_with_pool_returns_dict_on_parse_failure()
    {
        var pool = new DictionaryPool<string, string>(maxPoolSize: 4);
        var badJson = "not valid json"u8.ToArray();

        Assert.That(() => HeaderSerializer.Deserialize(badJson, pool), Throws.InstanceOf<JsonException>());

        // The failed-parse dictionary was returned to the pool (cleared).
        // A subsequent rent should succeed normally.
        var dict = pool.Rent();
        Assert.That(dict, Is.Empty);
    }
}