namespace NServiceBus
{
    using System;
    using System.Collections;
    using System.Collections.Generic;


    // Taken from https://github.com/StephenCleary/Deque/blob/master/src/Nito.Collections.Deque/Deque.cs
    // Can't use the package because it is net462 and this currently targets 452
    class Deque<T> : IList<T>, IReadOnlyList<T>, IList
    {
        const int DefaultCapacity = 8;
        T[] _buffer;
        int _offset;

        public Deque(int capacity)
        {
            if (capacity < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(capacity), "Capacity may not be negative.");
            }

            _buffer = new T[capacity];
        }

        public Deque(IEnumerable<T> collection)
        {
            if (collection == null)
            {
                throw new ArgumentNullException(nameof(collection));
            }

            var source = CollectionHelpers.ReifyCollection(collection);
            var count = source.Count;
            if (count > 0)
            {
                _buffer = new T[count];
                DoInsertRange(0, source);
            }
            else
            {
                _buffer = new T[DefaultCapacity];
            }
        }

        public Deque()
            : this(DefaultCapacity)
        {
        }

        bool ICollection<T>.IsReadOnly => false;

        public T this[int index]
        {
            get
            {
                CheckExistingIndexArgument(Count, index);
                return DoGetItem(index);
            }

            set
            {
                CheckExistingIndexArgument(Count, index);
                DoSetItem(index, value);
            }
        }

        public void Insert(int index, T item)
        {
            CheckNewIndexArgument(Count, index);
            DoInsert(index, item);
        }

        public void RemoveAt(int index)
        {
            CheckExistingIndexArgument(Count, index);
            DoRemoveAt(index);
        }

        public int IndexOf(T item)
        {
            var comparer = EqualityComparer<T>.Default;
            int ret = 0;
            foreach (var sourceItem in this)
            {
                if (comparer.Equals(item, sourceItem))
                {
                    return ret;
                }

                ++ret;
            }

            return -1;
        }

        void ICollection<T>.Add(T item)
        {
            DoInsert(Count, item);
        }

        bool ICollection<T>.Contains(T item)
        {
            var comparer = EqualityComparer<T>.Default;
            foreach (var entry in this)
            {
                if (comparer.Equals(item, entry))
                {
                    return true;
                }
            }
            return false;
        }

        void ICollection<T>.CopyTo(T[] array, int arrayIndex)
        {
            if (array == null)
            {
                throw new ArgumentNullException(nameof(array));
            }

            int count = Count;
            CheckRangeArguments(array.Length, arrayIndex, count);
            CopyToArray(array, arrayIndex);
        }

        void CopyToArray(Array array, int arrayIndex = 0)
        {
            if (array == null)
            {
                throw new ArgumentNullException(nameof(array));
            }

            if (IsSplit)
            {
                // The existing buffer is split, so we have to copy it in parts
                int length = Capacity - _offset;
                Array.Copy(_buffer, _offset, array, arrayIndex, length);
                Array.Copy(_buffer, 0, array, arrayIndex + length, Count - length);
            }
            else
            {
                // The existing buffer is whole
                Array.Copy(_buffer, _offset, array, arrayIndex, Count);
            }
        }

        public bool Remove(T item)
        {
            int index = IndexOf(item);
            if (index == -1)
            {
                return false;
            }

            DoRemoveAt(index);
            return true;
        }

        public IEnumerator<T> GetEnumerator()
        {
            int count = Count;
            for (int i = 0; i != count; ++i)
            {
                yield return DoGetItem(i);
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        static bool IsT(object value)
        {
            if (value is T)
            {
                return true;
            }

            if (value != null)
            {
                return false;
            }

            return default(T) == null;
        }

        int IList.Add(object value)
        {
            if (value == null && default(T) != null)
            {
                throw new ArgumentNullException(nameof(value), "Value cannot be null.");
            }

            if (!IsT(value))
            {
                throw new ArgumentException("Value is of incorrect type.", nameof(value));
            }

            AddToBack((T)value);
            return Count - 1;
        }

        bool IList.Contains(object value)
        {
            return IsT(value) && ((ICollection<T>)this).Contains((T)value);
        }

        int IList.IndexOf(object value)
        {
            return IsT(value) ? IndexOf((T)value) : -1;
        }

        void IList.Insert(int index, object value)
        {
            if (value == null && default(T) != null)
            {
                throw new ArgumentNullException(nameof(value), "Value cannot be null.");
            }

            if (!IsT(value))
            {
                throw new ArgumentException("Value is of incorrect type.", nameof(value));
            }

            Insert(index, (T)value);
        }

        bool IList.IsFixedSize => false;

        bool IList.IsReadOnly => false;

        void IList.Remove(object value)
        {
            if (IsT(value))
            {
                Remove((T)value);
            }
        }

        object IList.this[int index]
        {
            get
            {
                return this[index];
            }

            set
            {
                if (value == null && default(T) != null)
                {
                    throw new ArgumentNullException(nameof(value), "Value cannot be null.");
                }

                if (!IsT(value))
                {
                    throw new ArgumentException("Value is of incorrect type.", nameof(value));
                }

                this[index] = (T)value;
            }
        }

        void ICollection.CopyTo(Array array, int index)
        {
            if (array == null)
            {
                throw new ArgumentNullException(nameof(array), "Destination array cannot be null.");
            }

            CheckRangeArguments(array.Length, index, Count);

            try
            {
                CopyToArray(array, index);
            }
            catch (ArrayTypeMismatchException ex)
            {
                throw new ArgumentException("Destination array is of incorrect type.", nameof(array), ex);
            }
            catch (RankException ex)
            {
                throw new ArgumentException("Destination array must be single dimensional.", nameof(array), ex);
            }
        }

        bool ICollection.IsSynchronized => false;

        object ICollection.SyncRoot => this;

        static void CheckNewIndexArgument(int sourceLength, int index)
        {
            if (index < 0 || index > sourceLength)
            {
                throw new ArgumentOutOfRangeException(nameof(index), "Invalid new index " + index + " for source length " + sourceLength);
            }
        }

        static void CheckExistingIndexArgument(int sourceLength, int index)
        {
            if (index < 0 || index >= sourceLength)
            {
                throw new ArgumentOutOfRangeException(nameof(index), "Invalid existing index " + index + " for source length " + sourceLength);
            }
        }

        static void CheckRangeArguments(int sourceLength, int offset, int count)
        {
            if (offset < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(offset), "Invalid offset " + offset);
            }

            if (count < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(count), "Invalid count " + count);
            }

            if (sourceLength - offset < count)
            {
                throw new ArgumentException("Invalid offset (" + offset + ") or count + (" + count + ") for source length " + sourceLength);
            }
        }

        bool IsEmpty => Count == 0;

        bool IsFull => Count == Capacity;

        bool IsSplit =>
                // Overflow-safe version of "(offset + Count) > Capacity"
                _offset > (Capacity - Count);

        public int Capacity
        {
            get
            {
                return _buffer.Length;
            }

            set
            {
                if (value < Count)
                {
                    throw new ArgumentOutOfRangeException(nameof(value), "Capacity cannot be set to a value less than Count");
                }

                if (value == _buffer.Length)
                {
                    return;
                }

                // Create the new _buffer and copy our existing range.
                var newBuffer = new T[value];
                CopyToArray(newBuffer);

                // Set up to use the new _buffer.
                _buffer = newBuffer;
                _offset = 0;
            }
        }

        public int Count { get; private set; }

        int DequeIndexToBufferIndex(int index)
        {
            return (index + _offset) % Capacity;
        }

        T DoGetItem(int index)
        {
            return _buffer[DequeIndexToBufferIndex(index)];
        }

        void DoSetItem(int index, T item)
        {
            _buffer[DequeIndexToBufferIndex(index)] = item;
        }

        void DoInsert(int index, T item)
        {
            EnsureCapacityForOneElement();

            if (index == 0)
            {
                DoAddToFront(item);
                return;
            }
            else if (index == Count)
            {
                DoAddToBack(item);
                return;
            }

            DoInsertRange(index, new[] { item });
        }

        void DoRemoveAt(int index)
        {
            if (index == 0)
            {
                DoRemoveFromFront();
                return;
            }
            else if (index == Count - 1)
            {
                DoRemoveFromBack();
                return;
            }

            DoRemoveRange(index, 1);
        }

        int PostIncrement(int value)
        {
            int ret = _offset;
            _offset += value;
            _offset %= Capacity;
            return ret;
        }

        int PreDecrement(int value)
        {
            _offset -= value;
            if (_offset < 0)
            {
                _offset += Capacity;
            }

            return _offset;
        }

        void DoAddToBack(T value)
        {
            _buffer[DequeIndexToBufferIndex(Count)] = value;
            ++Count;
        }

        void DoAddToFront(T value)
        {
            _buffer[PreDecrement(1)] = value;
            ++Count;
        }

        T DoRemoveFromBack()
        {
            T ret = _buffer[DequeIndexToBufferIndex(Count - 1)];
            --Count;
            return ret;
        }

        T DoRemoveFromFront()
        {
            --Count;
            return _buffer[PostIncrement(1)];
        }

        void DoInsertRange(int index, IReadOnlyCollection<T> collection)
        {
            var collectionCount = collection.Count;
            // Make room in the existing list
            if (index < Count / 2)
            {
                // Inserting into the first half of the list

                // Move lower items down: [0, index) -> [Capacity - collectionCount, Capacity - collectionCount + index)
                // This clears out the low "index" number of items, moving them "collectionCount" places down;
                //   after rotation, there will be a "collectionCount"-sized hole at "index".
                int copyCount = index;
                int writeIndex = Capacity - collectionCount;
                for (int j = 0; j != copyCount; ++j)
                {
                    _buffer[DequeIndexToBufferIndex(writeIndex + j)] = _buffer[DequeIndexToBufferIndex(j)];
                }

                // Rotate to the new view
                PreDecrement(collectionCount);
            }
            else
            {
                // Inserting into the second half of the list

                // Move higher items up: [index, count) -> [index + collectionCount, collectionCount + count)
                int copyCount = Count - index;
                int writeIndex = index + collectionCount;
                for (int j = copyCount - 1; j != -1; --j)
                {
                    _buffer[DequeIndexToBufferIndex(writeIndex + j)] = _buffer[DequeIndexToBufferIndex(index + j)];
                }
            }

            // Copy new items into place
            int i = index;
            foreach (T item in collection)
            {
                _buffer[DequeIndexToBufferIndex(i)] = item;
                ++i;
            }

            // Adjust valid count
            Count += collectionCount;
        }

        void DoRemoveRange(int index, int collectionCount)
        {
            if (index == 0)
            {
                // Removing from the beginning: rotate to the new view
                PostIncrement(collectionCount);
                Count -= collectionCount;
                return;
            }
            else if (index == Count - collectionCount)
            {
                // Removing from the ending: trim the existing view
                Count -= collectionCount;
                return;
            }

            if ((index + (collectionCount / 2)) < Count / 2)
            {
                // Removing from first half of list

                // Move lower items up: [0, index) -> [collectionCount, collectionCount + index)
                int copyCount = index;
                int writeIndex = collectionCount;
                for (int j = copyCount - 1; j != -1; --j)
                {
                    _buffer[DequeIndexToBufferIndex(writeIndex + j)] = _buffer[DequeIndexToBufferIndex(j)];
                }

                // Rotate to new view
                PostIncrement(collectionCount);
            }
            else
            {
                // Removing from second half of list

                // Move higher items down: [index + collectionCount, count) -> [index, count - collectionCount)
                int copyCount = Count - collectionCount - index;
                int readIndex = index + collectionCount;
                for (int j = 0; j != copyCount; ++j)
                {
                    _buffer[DequeIndexToBufferIndex(index + j)] = _buffer[DequeIndexToBufferIndex(readIndex + j)];
                }
            }

            // Adjust valid count
            Count -= collectionCount;
        }

        void EnsureCapacityForOneElement()
        {
            if (IsFull)
            {
                Capacity = (Capacity == 0) ? 1 : Capacity * 2;
            }
        }

        public void AddToBack(T value)
        {
            EnsureCapacityForOneElement();
            DoAddToBack(value);
        }

        public void AddToFront(T value)
        {
            EnsureCapacityForOneElement();
            DoAddToFront(value);
        }

        public void InsertRange(int index, IEnumerable<T> collection)
        {
            CheckNewIndexArgument(Count, index);
            var source = CollectionHelpers.ReifyCollection(collection);
            int collectionCount = source.Count;

            // Overflow-safe check for "Count + collectionCount > Capacity"
            if (collectionCount > Capacity - Count)
            {
                Capacity = checked(Count + collectionCount);
            }

            if (collectionCount == 0)
            {
                return;
            }

            DoInsertRange(index, source);
        }

        public void RemoveRange(int offset, int count)
        {
            CheckRangeArguments(Count, offset, count);

            if (count == 0)
            {
                return;
            }

            DoRemoveRange(offset, count);
        }

        public T RemoveFromBack()
        {
            if (IsEmpty)
            {
                throw new InvalidOperationException("The deque is empty.");
            }

            return DoRemoveFromBack();
        }

        public T RemoveFromFront()
        {
            if (IsEmpty)
            {
                throw new InvalidOperationException("The deque is empty.");
            }

            return DoRemoveFromFront();
        }

        public void Clear()
        {
            _offset = 0;
            Count = 0;
        }

        public T[] ToArray()
        {
            var result = new T[Count];
            ((ICollection<T>)this).CopyTo(result, 0);
            return result;
        }
    }
}