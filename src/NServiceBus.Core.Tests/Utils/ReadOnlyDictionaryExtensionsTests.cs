#nullable enable

namespace NServiceBus.Core.Tests.Utils;

using System.Collections.Generic;
using System.Collections.ObjectModel;
using NServiceBus.Utils;
using NUnit.Framework;

public class ReadOnlyDictionaryExtensionsTests
{
    [Test]
    public void CopyTo_copies_entries_from_a_dictionary_and_overwrites_existing_keys()
    {
        var source = new Dictionary<string, string> { ["a"] = "1", ["b"] = "2" };
        var destination = new Dictionary<string, string> { ["b"] = "old", ["c"] = "3" };

        source.CopyTo(destination);

        Assert.That(destination, Is.EquivalentTo(new Dictionary<string, string> { ["a"] = "1", ["b"] = "2", ["c"] = "3" }));
    }

    [Test]
    public void CopyTo_copies_entries_from_any_read_only_dictionary_and_overwrites_existing_keys()
    {
        IReadOnlyDictionary<string, string> source = new ReadOnlyDictionary<string, string>(new Dictionary<string, string> { ["a"] = "1", ["b"] = "2" });
        var destination = new Dictionary<string, string> { ["b"] = "old", ["c"] = "3" };

        source.CopyTo(destination);

        Assert.That(destination, Is.EquivalentTo(new Dictionary<string, string> { ["a"] = "1", ["b"] = "2", ["c"] = "3" }));
    }
}
