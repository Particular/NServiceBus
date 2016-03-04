namespace NServiceBus
{
    using System;
    using System.IO;

    // Code kindly provided by the mono project: https://github.com/jbevain/mono.reflection/blob/master/Mono.Reflection/Image.cs
    // Image.cs
    //
    // Author:
    //   Jb Evain (jbevain@novell.com)
    //
    // (C) 2009 - 2010 Novell, Inc. (http://www.novell.com)
    //
    // Permission is hereby granted, free of charge, to any person obtaining
    // a copy of this software and associated documentation files (the
    // "Software"), to deal in the Software without restriction, including
    // without limitation the rights to use, copy, modify, merge, publish,
    // distribute, sublicense, and/or sell copies of the Software, and to
    // permit persons to whom the Software is furnished to do so, subject to
    // the following conditions:
    //
    // The above copyright notice and this permission notice shall be
    // included in all copies or substantial portions of the Software.
    //
    // THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
    // EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
    // MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
    // NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
    // LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
    // OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
    // WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
    class Image : IDisposable
    {
        public enum CompilationMode
        {
            NativeOrInvalid,
            CLRx86,
            CLRx64
        }

        Image(Stream stream)
        {
            this.stream = stream;
        }

        public void Dispose()
        {
        }

        public static CompilationMode GetCompilationMode(string file)
        {
            Guard.AgainstNull(nameof(file), file);

            using (var stream = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                using (var image = new Image(stream))
                {
                    return image.GetCompilationMode();
                }
            }
        }

        CompilationMode GetCompilationMode()
        {
            if (stream.Length < 318)
            {
                return CompilationMode.NativeOrInvalid;
            }
            if (ReadUInt16() != 0x5a4d)
            {
                return CompilationMode.NativeOrInvalid;
            }
            if (!Advance(58))
            {
                return CompilationMode.NativeOrInvalid;
            }
            if (!MoveTo(ReadUInt32()))
            {
                return CompilationMode.NativeOrInvalid;
            }
            if (ReadUInt32() != 0x00004550)
            {
                return CompilationMode.NativeOrInvalid;
            }
            if (!Advance(20))
            {
                return CompilationMode.NativeOrInvalid;
            }

            var result = CompilationMode.NativeOrInvalid;
            switch (ReadUInt16())
            {
                case 0x10B:
                    if (Advance(206))
                    {
                        result = CompilationMode.CLRx86;
                    }

                    break;
                case 0x20B:
                    if (Advance(222))
                    {
                        result = CompilationMode.CLRx64;
                    }
                    break;
            }

            if (result == CompilationMode.NativeOrInvalid)
            {
                return result;
            }

            return ReadUInt32() != 0 ? result : CompilationMode.NativeOrInvalid;
        }

        bool Advance(int length)
        {
            if (stream.Position + length >= stream.Length)
            {
                return false;
            }

            stream.Seek(length, SeekOrigin.Current);
            return true;
        }

        bool MoveTo(uint position)
        {
            if (position >= stream.Length)
            {
                return false;
            }

            stream.Position = position;
            return true;
        }

        ushort ReadUInt16()
        {
            return (ushort) (stream.ReadByte()
                             | (stream.ReadByte() << 8));
        }

        uint ReadUInt32()
        {
            return (uint) (stream.ReadByte()
                           | (stream.ReadByte() << 8)
                           | (stream.ReadByte() << 16)
                           | (stream.ReadByte() << 24));
        }

        Stream stream;
    }
}