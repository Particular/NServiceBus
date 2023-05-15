namespace NServiceBus
{
    using System;
    using System.Buffers.Binary;
#if NETFRAMEWORK
    using System.Runtime.InteropServices;
    using System.Runtime.CompilerServices;
#endif

    /// <summary>
    /// Generates a Guid using http://www.informit.com/articles/article.asp?p=25862
    /// The Comb algorithm is designed to make the use of <see cref="Guid"/>s as Primary Keys, Foreign Keys, and Indexes nearly as efficient
    /// as <see cref="int"/>.
    /// </summary>
    /// <remarks>Original Source: https://github.com/nhibernate/nhibernate-core/blob/4.0.4.GA/src/NHibernate/Id/GuidCombGenerator.cs</remarks>
    static class CombGuid
    {
        public static Guid Generate() =>
            // Internal use, no need for DateTimeOffset
            Generate(Guid.NewGuid(), DateTime.UtcNow);

        internal static Guid Generate(Guid inputGuid, DateTime inputNow)
        {
            var newGuid = inputGuid;
            Span<byte> guidArray = stackalloc byte[16];
#if NET
            if (!newGuid.TryWriteBytes(guidArray))
#else
            if (TryWriteBytes(newGuid, guidArray))
#endif
            {

                guidArray = newGuid.ToByteArray();
            }

            var now = inputNow;

            // Get the days and milliseconds which will be used to build the byte string
            var days = new TimeSpan(now.Ticks - BaseDateTicks);
            var timeOfDay = now.TimeOfDay;

            // Convert to a byte array
            Span<byte> daysArray = stackalloc byte[sizeof(int)];
            // Reverse the bytes to match SQL Servers ordering
            if (BitConverter.IsLittleEndian)
            {
                BinaryPrimitives.WriteInt32BigEndian(daysArray, days.Days);
            }
            else
            {
                BinaryPrimitives.WriteInt32LittleEndian(daysArray, days.Days);
            }
            Span<byte> milliSecondsArray = stackalloc byte[sizeof(long)];
            // Reverse the bytes to match SQL Servers ordering
            // Note that SQL Server is accurate to 1/300th of a millisecond so we divide by 3.333333
            if (BitConverter.IsLittleEndian)
            {
                BinaryPrimitives.WriteInt64BigEndian(milliSecondsArray, (long)(timeOfDay.TotalMilliseconds / 3.333333));
            }
            else
            {
                BinaryPrimitives.WriteInt64LittleEndian(milliSecondsArray, (long)(timeOfDay.TotalMilliseconds / 3.333333));
            }

            // // Copy the bytes into the guid
            daysArray.Slice(daysArray.Length - 2).CopyTo(guidArray.Slice(10, 2));
            milliSecondsArray.Slice(milliSecondsArray.Length - 4).CopyTo(guidArray.Slice(12, 4));

#if NET
            return new Guid(guidArray);
#else
            if (!TryParseGuidBytes(guidArray, out Guid readGuid))
            {
                readGuid = new Guid(guidArray.ToArray());
            }
            return readGuid;
#endif
        }

#if NETFRAMEWORK

        static bool TryWriteBytes(Guid guid, Span<byte> buffer)
        {
            // Based on https://github.com/dotnet/runtime/blob/9129083c2fc6ef32479168f0555875b54aee4dfb/src/libraries/System.Private.CoreLib/src/System/Guid.cs#L836

            if (buffer.Length < 16)
            {
                return false;
            }

            if (BitConverter.IsLittleEndian)
            {
                MemoryMarshal.Write(buffer, ref guid);
                return true;
            }

            // slower path for BigEndian
            GuidData data = Unsafe.As<Guid, GuidData>(ref guid);

            buffer[15] = data.K; // hoist bounds checks
            BinaryPrimitives.WriteInt32LittleEndian(buffer, data.A);
            BinaryPrimitives.WriteInt16LittleEndian(buffer.Slice(4), data.B);
            BinaryPrimitives.WriteInt16LittleEndian(buffer.Slice(6), data.C);
            buffer[8] = data.D;
            buffer[9] = data.E;
            buffer[10] = data.F;
            buffer[11] = data.G;
            buffer[12] = data.H;
            buffer[13] = data.I;
            buffer[14] = data.J;
            return true;
        }

        // This struct has the fields layed out to be GUID-like in order to read the GUID fields
        // to efficiently write them into memory without having to deal with endianness
        // Do not rename or reorder the fields.
        readonly struct GuidData
        {
            public readonly int A;
            public readonly short B;
            public readonly short C;
            public readonly byte D;
            public readonly byte E;
            public readonly byte F;
            public readonly byte G;
            public readonly byte H;
            public readonly byte I;
            public readonly byte J;
            public readonly byte K;

            // Creates a new GUID like struct initialized to the value represented by the
            // arguments.  The bytes are specified like this to avoid endianness issues.
            public GuidData(int a, short b, short c, byte d, byte e, byte f, byte g, byte h, byte i, byte j, byte k)
            {
                A = a;
                B = b;
                C = c;
                D = d;
                E = e;
                F = f;
                G = g;
                H = h;
                I = i;
                J = j;
                K = k;
            }
        }

        static bool TryParseGuidBytes(ReadOnlySpan<byte> bytes, out Guid guid)
        {
            if (bytes.Length != GuidSizeInBytes)
            {
                guid = default;
                return false;
            }

            if (BitConverter.IsLittleEndian)
            {
                guid = MemoryMarshal.Read<Guid>(bytes);
                return true;
            }

            // copied from https://github.com/dotnet/runtime/blob/9129083c2fc6ef32479168f0555875b54aee4dfb/src/libraries/System.Private.CoreLib/src/System/Guid.cs#L49
            // slower path for BigEndian:
            byte k = bytes[15];  // hoist bounds checks
            int a = BinaryPrimitives.ReadInt32LittleEndian(bytes);
            short b = BinaryPrimitives.ReadInt16LittleEndian(bytes.Slice(4));
            short c = BinaryPrimitives.ReadInt16LittleEndian(bytes.Slice(6));
            byte d = bytes[8];
            byte e = bytes[9];
            byte f = bytes[10];
            byte g = bytes[11];
            byte h = bytes[12];
            byte i = bytes[13];
            byte j = bytes[14];

            guid = new Guid(a, b, c, d, e, f, g, h, i, j, k);
            return true;
        }

        const int GuidSizeInBytes = 16;
#endif

        // Represents new DateTime(1900, 1, 1).Ticks, while this would be more readable having a const here instead of
        // a static field the less readable version slightly improves the throughput
        const long BaseDateTicks = 599266080000000000; // new DateTime(1900, 1, 1).Ticks
    }
}