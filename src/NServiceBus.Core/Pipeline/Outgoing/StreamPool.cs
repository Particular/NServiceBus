namespace NServiceBus
{
    using System;
    using System.Collections.Concurrent;
    using System.IO;

    class StreamPool
    {
        [ThreadStatic]
        readonly ConcurrentBag<MemoryStream> pool = new ConcurrentBag<MemoryStream>();
        long startCapacity = 2048;
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

                instance.Position = 0;
                pool.Add(instance);
            }
            // MemoryStreams do not need to be disposed.
        }
    }
}