namespace NServiceBus.Core.Tests.Persistence.RavenDB.SagaPersister
{
    using System;

    public static class EqualityExtensions
    {
        public static bool EqualTo<T>(this T item, object obj, Func<T, T, bool> equals)
        {
            if (!(obj is T)) return false;

            var x = (T)obj;

            if (item != null && x == null) return false;

            if (item == null && x != null) return false;

            if (item == null && x == null) return true;

            return equals(item, x);
        }
    }
}