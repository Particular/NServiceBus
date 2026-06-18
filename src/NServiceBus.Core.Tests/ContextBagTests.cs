#nullable enable

namespace NServiceBus.Core.Tests;

using Extensibility;
using NUnit.Framework;
using System.Collections.Generic;

[TestFixture]
public class ContextBagTests
{
    [Test]
    public void Should_not_allow_storing_null_value()
    {
        var contextBag = new ContextBag();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(() => contextBag.Set<string>("NullValue", null!), Throws.ArgumentNullException);
            Assert.That(() => contextBag.SetOnRoot<string>("NullValue", null!), Throws.ArgumentNullException);
        }
    }

    [Test]
    public void Should_not_allow_storing_null_value_even_with_nullable_generic_type()
    {
        var contextBag = new ContextBag();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(() => contextBag.Set<string?>("NullValue", null), Throws.ArgumentNullException);
            Assert.That(() => contextBag.SetOnRoot<string?>("NullValue", null), Throws.ArgumentNullException);
        }
    }

    [Test]
    public void ShouldAllowMonkeyPatching()
    {
        var contextBag = new ContextBag();

        contextBag.Set("MonkeyPatch", "some string");

        _ = ((IReadOnlyContextBag)contextBag).TryGet("MonkeyPatch", out string? theValue);
        Assert.That(theValue, Is.EqualTo("some string"));
    }

    [Test]
    public void SetOnRoot_should_set_value_on_root_context()
    {
        const string key = "testkey";

        var root = new ContextBag();
        var intermediate = new ContextBag(root);
        var context = new ContextBag(intermediate);
        var fork = new ContextBag(intermediate);

        context.SetOnRoot(key, 42);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(root.Get<int>(key), Is.EqualTo(42), "should store value on root context");
            Assert.That(context.Get<int>(key), Is.EqualTo(42), "stored value should be readable in the writing context");
            Assert.That(fork.Get<int>(key), Is.EqualTo(42), "stored value should be visible to a forked context");
        }
    }

    [Test]
    public void Should_set_and_get_a_single_inline_entry()
    {
        var bag = new ContextBag();

        bag.Set("key1", "value1");

        Assert.That(bag.Get<string>("key1"), Is.EqualTo("value1"));
    }

    [Test]
    public void Should_set_and_get_four_inline_entries()
    {
        var bag = new ContextBag();

        for (var i = 1; i <= 4; i++)
        {
            bag.Set($"key{i}", $"value{i}");
        }

        using (Assert.EnterMultipleScope())
        {
            for (var i = 1; i <= 4; i++)
            {
                Assert.That(bag.Get<string>($"key{i}"), Is.EqualTo($"value{i}"));
            }
        }
    }

    [Test]
    public void Should_set_and_get_eight_inline_entries()
    {
        var bag = new ContextBag();

        for (var i = 1; i <= 8; i++)
        {
            bag.Set($"key{i}", $"value{i}");
        }

        using (Assert.EnterMultipleScope())
        {
            for (var i = 1; i <= 8; i++)
            {
                Assert.That(bag.Get<string>($"key{i}"), Is.EqualTo($"value{i}"));
            }
        }
    }

    [Test]
    public void Should_set_and_get_more_than_eight_entries()
    {
        var bag = new ContextBag();

        for (var i = 1; i <= 12; i++)
        {
            bag.Set($"key{i}", $"value{i}");
        }

        using (Assert.EnterMultipleScope())
        {
            for (var i = 1; i <= 12; i++)
            {
                Assert.That(bag.Get<string>($"key{i}"), Is.EqualTo($"value{i}"));
            }
        }
    }

    [Test]
    public void Should_update_an_existing_inline_key()
    {
        var bag = new ContextBag();

        bag.Set("key1", "original");
        bag.Set("key1", "updated");

        Assert.That(bag.Get<string>("key1"), Is.EqualTo("updated"));
    }

    [Test]
    public void Should_update_an_existing_stash_key()
    {
        var bag = new ContextBag();

        for (var i = 1; i <= 9; i++)
        {
            bag.Set($"key{i}", $"value{i}");
        }

        bag.Set("key9", "updated");

        Assert.That(bag.Get<string>("key9"), Is.EqualTo("updated"));
    }

    [Test]
    public void Should_remove_the_first_inline_key()
    {
        var bag = new ContextBag();

        bag.Set("key1", "value1");
        bag.Set("key2", "value2");
        bag.Set("key3", "value3");

        bag.Remove("key1");

        using (Assert.EnterMultipleScope())
        {
            Assert.That(() => bag.Get<string>("key1"), Throws.TypeOf<KeyNotFoundException>());
            Assert.That(bag.Get<string>("key2"), Is.EqualTo("value2"));
            Assert.That(bag.Get<string>("key3"), Is.EqualTo("value3"));
        }
    }

    [Test]
    public void Should_remove_a_middle_inline_key()
    {
        var bag = new ContextBag();

        bag.Set("key1", "value1");
        bag.Set("key2", "value2");
        bag.Set("key3", "value3");

        bag.Remove("key2");

        using (Assert.EnterMultipleScope())
        {
            Assert.That(() => bag.Get<string>("key2"), Throws.TypeOf<KeyNotFoundException>());
            Assert.That(bag.Get<string>("key1"), Is.EqualTo("value1"));
            Assert.That(bag.Get<string>("key3"), Is.EqualTo("value3"));
        }
    }

    [Test]
    public void Should_remove_the_last_inline_key()
    {
        var bag = new ContextBag();

        bag.Set("key1", "value1");
        bag.Set("key2", "value2");
        bag.Set("key3", "value3");

        bag.Remove("key3");

        using (Assert.EnterMultipleScope())
        {
            Assert.That(() => bag.Get<string>("key3"), Throws.TypeOf<KeyNotFoundException>());
            Assert.That(bag.Get<string>("key1"), Is.EqualTo("value1"));
            Assert.That(bag.Get<string>("key2"), Is.EqualTo("value2"));
        }
    }

    [Test]
    public void Should_remove_a_stash_key()
    {
        var bag = new ContextBag();

        for (var i = 1; i <= 9; i++)
        {
            bag.Set($"key{i}", $"value{i}");
        }

        bag.Remove("key9");

        using (Assert.EnterMultipleScope())
        {
            Assert.That(() => bag.Get<string>("key9"), Throws.TypeOf<KeyNotFoundException>());
            for (var i = 1; i <= 8; i++)
            {
                Assert.That(bag.Get<string>($"key{i}"), Is.EqualTo($"value{i}"));
            }
        }
    }

    [Test]
    public void Should_keep_remaining_inline_keys_after_swap_removal()
    {
        var bag = new ContextBag();

        bag.Set("key1", "value1");
        bag.Set("key2", "value2");
        bag.Set("key3", "value3");
        bag.Set("key4", "value4");

        bag.Remove("key1");

        using (Assert.EnterMultipleScope())
        {
            Assert.That(() => bag.Get<string>("key1"), Throws.TypeOf<KeyNotFoundException>());
            Assert.That(bag.Get<string>("key2"), Is.EqualTo("value2"));
            Assert.That(bag.Get<string>("key3"), Is.EqualTo("value3"));
            Assert.That(bag.Get<string>("key4"), Is.EqualTo("value4"));
        }

        bag.Remove("key2");

        using (Assert.EnterMultipleScope())
        {
            Assert.That(() => bag.Get<string>("key1"), Throws.TypeOf<KeyNotFoundException>());
            Assert.That(() => bag.Get<string>("key2"), Throws.TypeOf<KeyNotFoundException>());
            Assert.That(bag.Get<string>("key3"), Is.EqualTo("value3"));
            Assert.That(bag.Get<string>("key4"), Is.EqualTo("value4"));
        }
    }

    [Test]
    public void Should_clear_the_bag()
    {
        var bag = new ContextBag();

        for (var i = 1; i <= 12; i++)
        {
            bag.Set($"key{i}", $"value{i}");
        }

        bag.Clear();

        using (Assert.EnterMultipleScope())
        {
            for (var i = 1; i <= 12; i++)
            {
                Assert.That(() => bag.Get<string>($"key{i}"), Throws.TypeOf<KeyNotFoundException>());
            }
        }
    }

    [Test]
    public void Should_throw_when_getting_a_missing_key()
    {
        var bag = new ContextBag();

        Assert.That(() => bag.Get<string>("nonexistent"), Throws.TypeOf<KeyNotFoundException>());
    }

    [Test]
    public void Should_return_false_when_try_getting_a_missing_key()
    {
        var bag = new ContextBag();

        var found = bag.TryGet<string>("nonexistent", out var result);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(found, Is.False);
            Assert.That(result, Is.Null);
        }
    }

    [Test]
    public void Should_find_a_key_in_the_parent_bag()
    {
        var root = new ContextBag();
        var child = new ContextBag(root);

        root.Set("parentKey", "parentValue");

        Assert.That(child.Get<string>("parentKey"), Is.EqualTo("parentValue"));
    }

    [Test]
    public void Should_fallback_to_parent_after_removing_local_override()
    {
        var root = new ContextBag();
        var child = new ContextBag(root);

        root.Set("sharedKey", "rootValue");
        child.Set("sharedKey", "childValue");

        child.Remove("sharedKey");

        Assert.That(child.Get<string>("sharedKey"), Is.EqualTo("rootValue"));
    }

    [Test]
    public void Should_allow_removing_a_nonexistent_key()
    {
        var bag = new ContextBag();

        Assert.That(() => bag.Remove("nonexistent"), Throws.Nothing);
    }

    [Test]
    public void Should_keep_inline_entries_reachable_after_overflowing_to_stash()
    {
        var bag = new ContextBag();

        for (var i = 1; i <= 8; i++)
        {
            bag.Set($"key{i}", $"inline{i}");
        }

        bag.Set("key9", "stashValue");

        using (Assert.EnterMultipleScope())
        {
            for (var i = 1; i <= 8; i++)
            {
                Assert.That(bag.Get<string>($"key{i}"), Is.EqualTo($"inline{i}"));
            }

            Assert.That(bag.Get<string>("key9"), Is.EqualTo("stashValue"));
        }
    }

    [Test]
    public void Should_reuse_inline_slots_after_removing_a_key()
    {
        var bag = new ContextBag();

        bag.Set("key1", "value1");
        bag.Set("key2", "value2");
        bag.Set("key3", "value3");

        bag.Remove("key2");

        bag.Set("key4", "value4");

        using (Assert.EnterMultipleScope())
        {
            Assert.That(bag.Get<string>("key1"), Is.EqualTo("value1"));
            Assert.That(bag.Get<string>("key3"), Is.EqualTo("value3"));
            Assert.That(bag.Get<string>("key4"), Is.EqualTo("value4"));
        }
    }

    [Test]
    public void Should_merge_inline_entries_into_empty_target()
    {
        var target = new ContextBag();
        var source = new ContextBag();

        source.Set("key1", "value1");
        source.Set("key2", "value2");
        source.Set("key3", "value3");

        target.Merge(source);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(target.Get<string>("key1"), Is.EqualTo("value1"));
            Assert.That(target.Get<string>("key2"), Is.EqualTo("value2"));
            Assert.That(target.Get<string>("key3"), Is.EqualTo("value3"));
        }
    }

    [Test]
    public void Should_merge_inline_entries_into_non_empty_target()
    {
        var target = new ContextBag();
        var source = new ContextBag();

        target.Set("existing", "existingValue");
        source.Set("key1", "value1");

        target.Merge(source);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(target.Get<string>("existing"), Is.EqualTo("existingValue"));
            Assert.That(target.Get<string>("key1"), Is.EqualTo("value1"));
        }
    }

    [Test]
    public void Should_merge_overwrites_existing_key()
    {
        var target = new ContextBag();
        var source = new ContextBag();

        target.Set("sharedKey", "originalValue");
        source.Set("sharedKey", "newValue");

        target.Merge(source);

        Assert.That(target.Get<string>("sharedKey"), Is.EqualTo("newValue"));
    }

    [Test]
    public void Should_merge_stash_entries_into_empty_target()
    {
        var target = new ContextBag();
        var source = new ContextBag();

        for (var i = 1; i <= 9; i++)
        {
            source.Set($"key{i}", $"value{i}");
        }

        target.Merge(source);

        using (Assert.EnterMultipleScope())
        {
            for (var i = 1; i <= 9; i++)
            {
                Assert.That(target.Get<string>($"key{i}"), Is.EqualTo($"value{i}"));
            }
        }
    }

    [Test]
    public void Should_merge_stash_entries_into_target_with_full_inline()
    {
        var target = new ContextBag();
        var source = new ContextBag();

        for (var i = 1; i <= 8; i++)
        {
            target.Set($"existing{i}", $"existingValue{i}");
        }

        for (var i = 1; i <= 9; i++)
        {
            source.Set($"key{i}", $"value{i}");
        }

        target.Merge(source);

        using (Assert.EnterMultipleScope())
        {
            for (var i = 1; i <= 8; i++)
            {
                Assert.That(target.Get<string>($"existing{i}"), Is.EqualTo($"existingValue{i}"));
            }

            for (var i = 1; i <= 9; i++)
            {
                Assert.That(target.Get<string>($"key{i}"), Is.EqualTo($"value{i}"));
            }
        }
    }

    [Test]
    public void Should_merge_stash_entries_fill_remaining_inline_slots()
    {
        var target = new ContextBag();
        var source = new ContextBag();

        target.Set("existing", "existingValue");

        for (var i = 1; i <= 9; i++)
        {
            source.Set($"key{i}", $"value{i}");
        }

        target.Merge(source);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(target.Get<string>("existing"), Is.EqualTo("existingValue"));

            for (var i = 1; i <= 9; i++)
            {
                Assert.That(target.Get<string>($"key{i}"), Is.EqualTo($"value{i}"));
            }
        }
    }

    [Test]
    public void Should_merge_when_both_have_stash_entries()
    {
        var target = new ContextBag();
        var source = new ContextBag();

        for (var i = 1; i <= 9; i++)
        {
            target.Set($"existing{i}", $"existingValue{i}");
        }

        for (var i = 1; i <= 9; i++)
        {
            source.Set($"key{i}", $"value{i}");
        }

        target.Merge(source);

        using (Assert.EnterMultipleScope())
        {
            for (var i = 1; i <= 9; i++)
            {
                Assert.That(target.Get<string>($"existing{i}"), Is.EqualTo($"existingValue{i}"));
            }

            for (var i = 1; i <= 9; i++)
            {
                Assert.That(target.Get<string>($"key{i}"), Is.EqualTo($"value{i}"));
            }
        }
    }

    [Test]
    public void Should_merge_from_empty_source()
    {
        var target = new ContextBag();
        var source = new ContextBag();

        target.Set("key1", "value1");

        target.Merge(source);

        Assert.That(target.Get<string>("key1"), Is.EqualTo("value1"));
    }
}