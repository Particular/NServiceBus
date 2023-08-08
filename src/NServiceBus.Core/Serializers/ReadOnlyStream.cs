namespace NServiceBus
{
    using System;
    using System.IO;

    sealed class ReadOnlyStream : Stream
    {
        readonly ReadOnlyMemory<byte> memory;
        int position;

        public ReadOnlyStream(ReadOnlyMemory<byte> memory)
        {
            this.memory = memory;
            position = 0;
        }

        public override void Flush() => throw new NotSupportedException();

        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();

        public override void SetLength(long value) => throw new NotSupportedException();

        public override int Read(byte[] buffer, int offset, int count)
        {
            var bytesToCopy = Math.Min(count, memory.Length - position);

            var destination = buffer.AsSpan().Slice(offset, bytesToCopy);
            var source = memory.Span.Slice(position, bytesToCopy);

            source.CopyTo(destination);

            position += bytesToCopy;

            return bytesToCopy;
        }

        public override int Read(Span<byte> buffer)
        {
            var bytesToCopy = Math.Min(memory.Length - position, buffer.Length);
            if (bytesToCopy <= 0)
            {
                return 0;
            }

            var source = memory.Span.Slice(position, bytesToCopy);
            source.CopyTo(buffer);

            position += bytesToCopy;
            return bytesToCopy;
        }

        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();

        public override bool CanRead => true;
        public override bool CanSeek => false;
        public override bool CanWrite => false;
        public override long Length => memory.Length;
        public override long Position { get => position; set => position = (int)value; }
    }
}