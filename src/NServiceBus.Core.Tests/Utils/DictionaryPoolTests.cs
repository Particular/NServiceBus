#nullable enable

namespace NServiceBus.Core.Tests.Utils;

using System;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
using System.Linq;
using System.Threading.Tasks;
using NServiceBus.Core.Tests.Helpers;
using NServiceBus.Transport;
using NServiceBus.Utils;
using NUnit.Framework;

public class DictionaryPoolTests
{
    [Test]
    public void Rent_returns_empty_dictionary()
    {
        var pool = new DictionaryPool<string, string>(maxPoolSize: 4);
        var dict = pool.Rent();
        Assert.That(dict, Is.Not.Null);
        Assert.That(dict, Is.Empty);
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
        Assert.That(reused, Is.Empty);
    }

    [Test]
    public void Rent_with_minimum_capacity_prevents_resize()
    {
        var pool = new DictionaryPool<string, string>(maxPoolSize: 4);
        var dict = pool.Rent(minimumCapacity: 100);
        Assert.That(dict, Is.Empty);

        for (int i = 0; i < 100; i++)
        {
            dict[$"key{i}"] = $"value{i}";
        }
        Assert.That(dict, Has.Count.EqualTo(100));
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
        Assert.That(reused, Is.Empty, "Cleared dictionary must be empty.");
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
        Assert.That(reused, Has.Count.EqualTo(1));
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

        using (Assert.EnterMultipleScope())
        {
            Assert.That(r1, Is.SameAs(d2).Or.SameAs(d1), "First rent should return a pooled dictionary.");
            Assert.That(r2, Is.SameAs(d2).Or.SameAs(d1), "Second rent should return a pooled dictionary.");
        }
        Assert.That(r1, Is.Not.SameAs(r2), "The two rents should return different dictionaries.");

        // The third rent must NOT return d3 — it was dropped by the cap.
        var r3 = pool.Rent();
        Assert.That(r3, Is.Not.SameAs(d3), "Dropped dictionary must not be returned from the pool.");
        Assert.That(r3, Is.Empty, "Fresh rent must be empty.");
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
        Assert.That(reused, Is.Empty);
    }

    [Test]
    public void PoolId_is_unique_per_pool_instance_and_stable()
    {
        var pool1 = new DictionaryPool<string, string>();
        var pool2 = new DictionaryPool<string, string>();
        var firstRead = pool1.PoolId;

        Assert.That(pool1.PoolId, Is.Not.EqualTo(pool2.PoolId), "Each pool instance must have a unique PoolId.");
        Assert.That(pool1.PoolId, Is.EqualTo(firstRead), "PoolId must be stable across reads.");
    }

    [Test]
    public void Event_source_reports_pool_creation_with_key_and_value_types()
    {
        using var events = new EventListenerScope("NServiceBus.DictionaryPool", EventLevel.Informational);

        var pool = new DictionaryPool<string, int>();

        Assert.That(events, Has.Count.EqualTo(1), "Expected exactly one DictionaryPoolCreated event.");
        using (Assert.EnterMultipleScope())
        {
            Assert.That(events.First().EventId, Is.EqualTo(6), "Expected DictionaryPoolCreated.");
            Assert.That(events.First().Payload, Is.EqualTo(new object[] { pool.PoolId, "System.String", "System.Int32" }),
                "DictionaryPoolCreated: poolId, key type, value type.");
        }
    }

    [Test]
    public void Event_source_reports_rent_allocate_and_return()
    {
        var pool = new DictionaryPool<string, string>(maxPoolSize: 4);

        using var events = new EventListenerScope("NServiceBus.DictionaryPool", EventLevel.Verbose);

        // The pool starts empty, so the first rent allocates; the return is retained.
        var dict = pool.Rent(minimumCapacity: 10);
        for (int i = 0; i < 10; i++)
        {
            dict[$"key{i}"] = "value";
        }
        pool.Return(dict);

        Assert.That(events, Has.Count.EqualTo(3), "Expected exactly rented, allocated and returned events.");
        using (Assert.EnterMultipleScope())
        {
            Assert.That(events.Count(e => e.EventId == 1), Is.EqualTo(1), "Expected a single DictionaryRented event.");
            Assert.That(events.Count(e => e.EventId == 2), Is.EqualTo(1), "Expected a single DictionaryAllocated event.");
            Assert.That(events.Count(e => e.EventId == 3), Is.EqualTo(1), "Expected a single DictionaryReturned event.");
            Assert.That(events.First(e => e.EventId == 1).Payload, Is.EqualTo(new object[] { pool.PoolId, 10 }),
                "DictionaryRented: poolId, minimumCapacity.");
            Assert.That(events.First(e => e.EventId == 2).Payload, Is.EqualTo(new object[] { pool.PoolId, 10 }),
                "DictionaryAllocated: poolId, minimumCapacity.");
            Assert.That(events.First(e => e.EventId == 3).Payload, Is.EqualTo(new object[] { pool.PoolId, 10 }),
                "DictionaryReturned: poolId, entry count.");
        }
    }

    [Test]
    public void Event_source_reports_trim_and_drop()
    {
        var pool = new DictionaryPool<string, string>(maxPoolSize: 1, maxRetainedCapacityPerItem: 5);

        // Two in-flight dictionaries: one oversized, one empty.
        var oversized = pool.Rent();
        for (int i = 0; i < 10; i++)
        {
            oversized[$"key{i}"] = "value";
        }
        var dropped = pool.Rent();

        using var events = new EventListenerScope("NServiceBus.DictionaryPool", EventLevel.Verbose);

        // The oversized dictionary is trimmed and retained; the pool is then full,
        // so the empty dictionary is dropped.
        pool.Return(oversized);
        pool.Return(dropped);

        Assert.That(events, Has.Count.EqualTo(3), "Expected exactly trimmed, returned and dropped events.");
        using (Assert.EnterMultipleScope())
        {
            Assert.That(events.Count(e => e.EventId == 4), Is.EqualTo(1), "Expected a single DictionaryTrimmed event.");
            Assert.That(events.Count(e => e.EventId == 3), Is.EqualTo(1), "Expected a single DictionaryReturned event.");
            Assert.That(events.Count(e => e.EventId == 5), Is.EqualTo(1), "Expected a single DictionaryDropped event.");
            Assert.That(events.First(e => e.EventId == 4).Payload, Is.EqualTo(new object[] { pool.PoolId, 10, 5 }),
                "DictionaryTrimmed: poolId, entry count, maxRetainedCapacity.");
            Assert.That(events.First(e => e.EventId == 3).Payload, Is.EqualTo(new object[] { pool.PoolId, 10 }),
                "DictionaryReturned: poolId, entry count.");
            Assert.That(events.First(e => e.EventId == 5).Payload, Is.EqualTo(new object[] { pool.PoolId, 0, 0 }),
                "DictionaryDropped: poolId, entry count, DictionaryDroppedReason.PoolFull.");
        }
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

        using (Assert.EnterMultipleScope())
        {
            Assert.That(failures, Is.Empty, $"{failures.Count} rent-return cycles saw stale data: {string.Join("; ", failures.Take(5))}");

            // The pool's approximate count must never exceed the cap.
            Assert.That(pool.Count, Is.LessThanOrEqualTo(64),
                $"Pool count {pool.Count} exceeded maxPoolSize of 64.");
        }

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

    [Test]
    public void Return_of_foreign_dictionary_is_a_no_op()
    {
        var pool = new DictionaryPool<string, string>(maxPoolSize: 4);
        var foreign = new Dictionary<string, string> { ["a"] = "1" };

        pool.Return(foreign);

        Assert.That(pool.Count, Is.Zero, "Foreign dictionary must not be pooled.");

        var rented = pool.Rent();
        Assert.That(rented, Is.Not.SameAs(foreign), "Rent must never hand out a dictionary the pool did not create.");
        using (Assert.EnterMultipleScope())
        {
            Assert.That(rented, Is.Empty, "Fresh rent must be empty.");
            Assert.That(foreign, Does.ContainKey("a"), "Foreign dictionary must not be cleared.");
        }
    }

    [Test]
    public void Double_return_is_a_no_op()
    {
        var pool = new DictionaryPool<string, string>(maxPoolSize: 4);
        var dict = pool.Rent();
        dict["a"] = "1";

        pool.Return(dict);
        pool.Return(dict);

        Assert.That(pool.Count, Is.EqualTo(1), "Double return must not retain the dictionary twice.");

        var reused = pool.Rent();
        Assert.That(reused, Is.SameAs(dict));
        Assert.That(reused, Is.Empty);

        // The recycled dictionary can be returned again after being rented out.
        reused["b"] = "2";
        pool.Return(reused);
        Assert.That(pool.Count, Is.EqualTo(1));
    }

    [Test]
    public void Always_allocate_rent_returns_fresh_dictionary_honoring_capacity()
    {
        var first = HeaderPool.AlwaysAllocate.Rent(minimumCapacity: 10);
        var second = HeaderPool.AlwaysAllocate.Rent(minimumCapacity: 10);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(first, Is.Not.SameAs(second), "AlwaysAllocate must never hand out the same instance twice.");
            Assert.That(first, Is.Empty);
            Assert.That(second, Is.Empty);
            Assert.That(first.Capacity, Is.GreaterThanOrEqualTo(10), "minimumCapacity must pre-size the dictionary.");
            Assert.That(second.Capacity, Is.GreaterThanOrEqualTo(10), "minimumCapacity must pre-size the dictionary.");
        }
    }

    [Test]
    public void Always_allocate_return_is_a_no_op()
    {
        var dict = HeaderPool.AlwaysAllocate.Rent();
        dict["a"] = "1";

        HeaderPool.AlwaysAllocate.Return(dict);

        Assert.That(dict, Does.ContainKey("a"), "A no-op return must not clear the dictionary.");
    }

    [Test]
    public void Always_allocate_return_discards_instances_rented_from_a_real_pool()
    {
        var realPool = new DictionaryPool<string, string>(maxPoolSize: 4);
        var rented = realPool.Rent();
        rented["a"] = "1";

        HeaderPool.AlwaysAllocate.Return(rented);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(rented, Does.ContainKey("a"), "Discard must not clear the dictionary.");
            Assert.That(realPool.Count, Is.Zero, "A dictionary discarded by AlwaysAllocate must not end up in any pool.");
        }

        var fresh = realPool.Rent();
        Assert.That(fresh, Is.Not.SameAs(rented), "Rent must allocate a fresh dictionary.");
    }
}