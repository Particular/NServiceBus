namespace NServiceBus
{
    using System.Collections.Concurrent;
    using System.IO;

    class StreamPool
    {
        readonly ConcurrentBag<MemoryStream> pool = new ConcurrentBag<MemoryStream>();
        long startCapacity;
        const int MaxSizeLimit = 1024 * 16; //16KB
        public MemoryStream Get()
        {
            if (pool.TryTake(out MemoryStream stream))
            {
                return stream;
            }
            return new MemoryStream((int)startCapacity);
        }

        public void Return(MemoryStream instance)
        {
            var position = instance.Position;
            if (position < MaxSizeLimit)
            {
                if (position > startCapacity)
                {
                    startCapacity = position;
                }
                pool.Add(instance);
            }
            // MemoryStreams do not need to be disposed.
        }
    }
}