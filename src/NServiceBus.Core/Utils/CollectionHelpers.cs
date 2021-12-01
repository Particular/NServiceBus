namespace NServiceBus
{
    using System;
    using System.Collections;
    using System.Collections.Generic;

    // Taken from https://github.com/StephenCleary/Deque/blob/master/src/Nito.Collections.Deque/CollectionHelpers.cs
    // Can't use the package because it is net462 and this currently targets 452
    static class CollectionHelpers
    {
        public static IReadOnlyCollection<T> ReifyCollection<T>(IEnumerable<T> source)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            if (source is IReadOnlyCollection<T> result)
            {
                return result;
            }

            if (source is ICollection<T> collection)
            {
                return new CollectionWrapper<T>(collection);
            }

            if (source is ICollection nongenericCollection)
            {
                return new NongenericCollectionWrapper<T>(nongenericCollection);
            }

            return new List<T>(source);
        }

        sealed class NongenericCollectionWrapper<T> : IReadOnlyCollection<T>
        {
            readonly ICollection _collection;

            public NongenericCollectionWrapper(ICollection collection)
            {
                _collection = collection ?? throw new ArgumentNullException(nameof(collection));
            }

            public int Count => _collection.Count;

            public IEnumerator<T> GetEnumerator()
            {
                foreach (T item in _collection)
                {
                    yield return item;
                }
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return _collection.GetEnumerator();
            }
        }

        sealed class CollectionWrapper<T> : IReadOnlyCollection<T>
        {
            readonly ICollection<T> _collection;

            public CollectionWrapper(ICollection<T> collection)
            {
                _collection = collection ?? throw new ArgumentNullException(nameof(collection));
            }

            public int Count => _collection.Count;

            public IEnumerator<T> GetEnumerator()
            {
                return _collection.GetEnumerator();
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return _collection.GetEnumerator();
            }
        }
    }
}
