namespace NServiceBus
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using System.Runtime.Serialization;
    using System.Text;

    sealed class MsmqFastHeadersParser
    {
        static MsmqFastHeadersParser()
        {
            var headerKeys = typeof(Headers).GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy)
                .Where(fi => fi.IsLiteral && !fi.IsInitOnly)
                .Select(fi => fi.GetRawConstantValue())
                .Cast<string>()
                .ToArray();

            KnownHeaders = new Dictionary<ArraySegment<byte>, string>(new KeyArraySegmentComparer());

            foreach (var headerKey in headerKeys)
            {
                KnownHeaders.Add(new ArraySegment<byte>(Encoding.UTF8.GetBytes(headerKey)), headerKey);
            }
        }

        MsmqFastHeadersParser(byte[] bytes)
        {
            this.bytes = bytes;
        }

        public static Dictionary<string, string> ParseHeaders(byte[] bytes)
        {
            return new MsmqFastHeadersParser(bytes).ParseHeaders();
        }

        Dictionary<string, string> ParseHeaders()
        {
            var result = new Dictionary<string, string>();
            var byteCount = bytes.Length;

            if (byteCount == 0)
            {
                return result;
            }

            // <xml
            if (TryMoveToNext(Open) == false)
            {
                return result;
            }
            pos += 1;

            // <ArrayOfHeaderInfo
            if (TryMoveToNext(Open) == false)
            {
                return result;
            }
            pos += 1;

            // loop over <HeaderInfo>, moving to the starting tag
            while (TryMoveToNext(Open))
            {
                if (TryConsumeTag(HeaderInfo) == false)
                {
                    // this is not a header info
                    break;
                }
                // inside <HeaderInfo>...
                //                    ^

                ConsumeTag(HeaderKey);
                // <Key>...
                //      ^
                var keyStart = pos;
                MoveToNext(Open);

                var key = GetKey(new ArraySegment<byte>(bytes, keyStart, pos - keyStart));

                ConsumeTag(HeaderKeyEnd);
                // </Key>...
                //       ^

                MoveToNext(Open);

                if (TryConsumeTag(HeaderValue))
                {
                    // <Value>...
                    //        ^

                    var valueStart = pos;
                    MoveToNext(Open);

                    var value = GetValue(new ArraySegment<byte>(bytes, valueStart, pos - valueStart));
                    result.Add(key, value);

                    TryConsumeTag(HeaderValueEnd);
                    // </Value>...
                    //         ^
                }
                else
                {
                    if (TryConsumeTag(HeaderValueEmpty))
                    {
                        // <Value />...
                        //          ^
                        result.Add(key, "");
                    }
                    else
                    {
                        throw new SerializationException();
                    }
                }
                ConsumeTag(HeaderInfoEnd);
                // </HeaderInfo>...
                //              ^
            }

            return result;
        }

        static string GetKey(ArraySegment<byte> s)
        {
            string key;
            return KnownHeaders.TryGetValue(s, out key) ? key : Encoding.UTF8.GetString(s.Array, s.Offset, s.Count);
        }

        static string GetValue(ArraySegment<byte> s)
        {
            return Encoding.UTF8.GetString(s.Array, s.Offset, s.Count);
        }

        /// <summary>
        /// Consumes a tag moving the <see cref="pos" /> rigth after the tag.
        /// </summary>
        void ConsumeTag(ArraySegment<byte> tag)
        {
            if (TryConsumeTag(tag) == false)
            {
                throw new SerializationException();
            }
        }

        bool TryConsumeTag(ArraySegment<byte> tag)
        {
            // preserve position in case of failure
            var tempPosition = pos;

            MoveToNext(Open);
            var start = pos + 1;
            MoveToNext(Close);
            var end = pos - 1;

            var headerInfo = new ArraySegment<byte>(bytes, start, end - start + 1);
            if (UnsafeCompare(headerInfo, tag))
            {
                pos += 1;
                return true;
            }

            pos = tempPosition;
            return false;
        }

        bool TryMoveToNext(byte b)
        {
            if (pos < 0)
            {
                return false;
            }
            pos = Array.IndexOf(bytes, b, pos);
            return true;
        }

        void MoveToNext(byte b)
        {
            if (TryMoveToNext(b) == false)
            {
                throw new SerializationException();
            }
        }

        static unsafe bool UnsafeCompare(ArraySegment<byte> a1, ArraySegment<byte> a2)
        {
            if (a1.Count != a2.Count)
                return false;
            fixed(byte* p1 = a1.Array, p2 = a2.Array)
            {
                var x1 = p1 + a1.Offset;
                var x2 = p2 + a2.Offset;
                var l = a1.Count;
                for (var i = 0; i < l/8; i++, x1 += 8, x2 += 8)
                    if (*((long*) x1) != *((long*) x2)) return false;
                if ((l & 4) != 0)
                {
                    if (*((int*) x1) != *((int*) x2)) return false;
                    x1 += 4;
                    x2 += 4;
                }
                if ((l & 2) != 0)
                {
                    if (*((short*) x1) != *((short*) x2)) return false;
                    x1 += 2;
                    x2 += 2;
                }
                if ((l & 1) != 0) if (*x1 != *x2) return false;
                return true;
            }
        }

        readonly byte[] bytes;
        int pos;

        static byte Open = Encoding.UTF8.GetBytes("<").Single();
        static byte Close = Encoding.UTF8.GetBytes(">").Single();
        static readonly ArraySegment<byte> HeaderInfo = new ArraySegment<byte>(Encoding.UTF8.GetBytes("HeaderInfo"));
        static readonly ArraySegment<byte> HeaderInfoEnd = new ArraySegment<byte>(Encoding.UTF8.GetBytes("/HeaderInfo"));
        static readonly ArraySegment<byte> HeaderKey = new ArraySegment<byte>(Encoding.UTF8.GetBytes("Key"));
        static readonly ArraySegment<byte> HeaderKeyEnd = new ArraySegment<byte>(Encoding.UTF8.GetBytes("/Key"));
        static readonly ArraySegment<byte> HeaderValue = new ArraySegment<byte>(Encoding.UTF8.GetBytes("Value"));
        static readonly ArraySegment<byte> HeaderValueEnd = new ArraySegment<byte>(Encoding.UTF8.GetBytes("/Value"));
        static readonly ArraySegment<byte> HeaderValueEmpty = new ArraySegment<byte>(Encoding.UTF8.GetBytes("Value /"));
        static Dictionary<ArraySegment<byte>, string> KnownHeaders;

        class KeyArraySegmentComparer : IEqualityComparer<ArraySegment<byte>>
        {
            public bool Equals(ArraySegment<byte> x, ArraySegment<byte> y)
            {
                return UnsafeCompare(x, y);
            }

            public int GetHashCode(ArraySegment<byte> obj)
            {
                return obj.Count;
            }
        }
    }
}