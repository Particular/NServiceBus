#nullable enable

namespace NServiceBus.Core.Tests.Utils;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using NServiceBus.Utils;

public class DictionaryPoolTests
{
    [Test]
    public void Rent_returns_empty_dictionary()
    {
        var pool = new DictionaryPool<string, string>(maxPoolSize: 4);
        var dict = pool.Rent();
        Assert.That(dict, Is.Not.Null);
        Assert.That(dict.Count, Is.EqualTo(0));
    }

    [Test]
    public void Returned_dictionary_is_reused_on_next_rent()
    {
        var pool = new DictionaryPool<string, string>(maxPoolSize: 4);
        var dict = pool.Rent();
        dict["a"] = "1";
        pool.Return(dict);

        var reused = pool.Rent();
        Assert.That(reused, Is.SameAs(dict));
        Assert.That(reused.Count, Is.EqualTo(0));
    }

    [Test]
    public void Rent_with_minimum_capacity_prevents_resize()
    {
        var pool = new DictionaryPool<string, string>(maxPoolSize: 4);
        var dict = pool.Rent(minimumCapacity: 100);
        Assert.That(dict.Count, Is.EqualTo(0));

        for (int i = 0; i < 100; i++)
        {
            dict[$"key{i}"] = $"value{i}";
        }
        Assert.That(dict.Count, Is.EqualTo(100));
    }

    [Test]
    public void Return_preserves_capacity_for_reuse_without_resize()
    {
        var pool = new DictionaryPool<string, string>(maxPoolSize: 4);

        // Fill a dictionary with a realistic header count so its internal
        // Entry[]/buckets grow to accommodate ~50 entries.
        var first = pool.Rent();
        for (int i = 0; i < 50; i++)
        {
            first[$"header-{i}"] = $"value-{i}";
        }
        var capacityAfterFill = first.Capacity;
        Assert.That(capacityAfterFill, Is.GreaterThanOrEqualTo(50),
            "Sanity: filling 50 entries should grow capacity to at least 50.");

        // Return → Clear() preserves the internal arrays (Capacity).
        pool.Return(first);

        // Rent again — should get the same instance with its capacity intact.
        var reused = pool.Rent();
        Assert.That(reused, Is.SameAs(first));
        Assert.That(reused.Count, Is.EqualTo(0), "Cleared dictionary must be empty.");
        Assert.That(reused.Capacity, Is.EqualTo(capacityAfterFill),
            "Clear() must preserve Capacity so the next rent avoids resize.");

        // Refill the same number of entries — capacity must not change,
        // proving no internal reallocation occurred.
        for (int i = 0; i < 50; i++)
        {
            reused[$"header-{i}"] = $"value-{i}";
        }
        Assert.That(reused.Capacity, Is.EqualTo(capacityAfterFill),
            "Refilling with the same count must not trigger a resize.");
    }

    [Test]
    public void Oversized_return_trims_capacity()
    {
        var pool = new DictionaryPool<string, string>(maxPoolSize: 4, maxRetainedCapacityPerItem: 5);
        var dict = pool.Rent();
        for (int i = 0; i < 100; i++)
        {
            dict[$"key{i}"] = $"value{i}";
        }
        pool.Return(dict); // Count=100 > 5 → Clear + TrimExcess

        var reused = pool.Rent();
        reused["x"] = "1";
        Assert.That(reused.Count, Is.EqualTo(1));
    }

    [Test]
    public void Soft_cap_drops_excess_returns_beyond_capacity()
    {
        var pool = new DictionaryPool<string, string>(maxPoolSize: 2);

        // Rent three distinct dictionaries without returning any,
        // so the pool is empty and each Rent allocates fresh.
        var d1 = pool.Rent();
        var d2 = pool.Rent();
        var d3 = pool.Rent();

        // Return all three. The first two bring the pool to its cap (2);
        // the third must be dropped, not retained.
        pool.Return(d1); // count → 1
        pool.Return(d2); // count → 2 (cap)
        pool.Return(d3); // exceeds cap → dropped, count stays 2

        // Rent twice — we should get d1 and d2 back (in LIFO order via ConcurrentBag).
        var r1 = pool.Rent();
        var r2 = pool.Rent();

        Assert.That(r1, Is.SameAs(d2).Or.SameAs(d1), "First rent should return a pooled dictionary.");
        Assert.That(r2, Is.SameAs(d2).Or.SameAs(d1), "Second rent should return a pooled dictionary.");
        Assert.That(r1, Is.Not.SameAs(r2), "The two rents should return different dictionaries.");

        // The third rent must NOT return d3 — it was dropped by the cap.
        var r3 = pool.Rent();
        Assert.That(r3, Is.Not.SameAs(d3), "Dropped dictionary must not be returned from the pool.");
        Assert.That(r3.Count, Is.EqualTo(0), "Fresh rent must be empty.");
    }

    [Test]
    public void Return_null_throws_argument_null_exception()
    {
        var pool = new DictionaryPool<string, string>(maxPoolSize: 4);
        Assert.Throws<ArgumentNullException>(() => pool.Return(null!));
    }

    [Test]
    public void Return_without_clear_preserves_data_for_caller_that_cleared()
    {
        var pool = new DictionaryPool<string, string>(maxPoolSize: 4);
        var dict = pool.Rent();
        dict["a"] = "1";

        // Caller is responsible for clearing when passing clearDictionary: false.
        dict.Clear();
        pool.Return(dict, clearDictionary: false);

        var reused = pool.Rent();
        Assert.That(reused, Is.SameAs(dict));
        Assert.That(reused.Count, Is.EqualTo(0));
    }

    [Test]
    public void Concurrent_rent_return_does_not_corrupt()
    {
        var pool = new DictionaryPool<string, string>(maxPoolSize: 64);
        var iterations = 5000;
        var failures = new System.Collections.Concurrent.ConcurrentQueue<string>();

        Parallel.For(0, iterations, i =>
        {
            var dict = pool.Rent();

            // Every rented dictionary must start empty — a stale entry
            // means Clear was skipped or a cross-thread race leaked data.
            if (dict.Count != 0)
            {
                failures.Enqueue($"Iteration {i}: rented dict had Count={dict.Count} (expected 0)");
                return;
            }

            // Write a unique key per iteration so collisions are detectable.
            dict[$"key-{i}"] = $"value-{i}";

            pool.Return(dict);
        });

        Assert.That(failures, Is.Empty, $"{failures.Count} rent-return cycles saw stale data: {string.Join("; ", failures.Take(5))}");

        // The pool's approximate count must never exceed the cap.
        Assert.That(pool.Count, Is.LessThanOrEqualTo(64),
            $"Pool count {pool.Count} exceeded maxPoolSize of 64.");

        // Drain the pool by renting until it's empty. Every returned dictionary
        // must be empty (Return must have cleared it).
        var drained = 0;
        while (pool.Count > 0)
        {
            var dict = pool.Rent();
            drained++;
            if (dict.Count != 0)
            {
                Assert.Fail($"Drained dictionary #{drained} had Count={dict.Count} (expected 0 after Return).");
            }
        }

        Assert.That(drained, Is.LessThanOrEqualTo(64), $"Pool retained {drained} dictionaries, exceeding maxPoolSize of 64.");
    }
}