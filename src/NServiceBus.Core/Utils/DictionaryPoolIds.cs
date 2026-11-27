#nullable enable

namespace NServiceBus;

using System.Threading;

/// <summary>
/// Allocates process-unique pool ids for <see cref="NServiceBus.Utils.DictionaryPool{TKey, TValue}"/>.
/// Defined outside the generic type so every closed generic type shares the same
/// id sequence; a static field on the generic type itself would give each closed
/// type its own sequence and thus collide across pools of different key/value types.
/// </summary>
static class DictionaryPoolIds
{
    static int nextId;

    internal static int Next() => Interlocked.Increment(ref nextId);
}
