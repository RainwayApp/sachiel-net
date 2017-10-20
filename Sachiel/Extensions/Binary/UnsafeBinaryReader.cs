using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace Sachiel.Extensions.Binary
{
    public unsafe class UnsafeBinaryReader
    {
        private static readonly Func<Decoder, bool> HasDecoderStateFunction = BuildHasDecoderStateFunction();
        private readonly Encoding _encoding;
        private readonly Decoder _decoder;

        private BufferSegment[] _buffers = new BufferSegment[1024];
        private int _bufferCount;
        private int _bufferIndex;
 
      

        private byte* _position;
        private byte* _endOfBuffer;
     
        public UnsafeBinaryReader(Encoding encoding)
        {
            _encoding = encoding;
            _decoder = _encoding.GetDecoder();
        }

        public void SetBuffer(byte* buffer, int length)
        {
            _bufferCount = 0;
            _bufferIndex = 0;

            _position = buffer;
            _endOfBuffer = buffer + length;
        }

        public void SetBuffers(List<BufferSegment> buffers)
        {
            if (buffers == null || buffers.Count == 0)
                throw new ArgumentException(nameof(buffers));

            _bufferCount = buffers.Count;
            _bufferIndex = 0;

            if (_bufferCount > _buffers.Length)
                _buffers = new BufferSegment[_bufferCount];

            for (var i = 0; i < buffers.Count; i++)
            {
                _buffers[i] = buffers[i];
            }

            var currentSegment = _buffers[_bufferIndex++];
            _position = currentSegment.Data;
            _endOfBuffer = _position + currentSegment.Length;
        }

        public bool ReadBoolean()
        {
            if (!CurrentBufferHasEnoughBytes(sizeof(bool)))
                return ReadOverlapped(sizeof(byte)) != 0;

            var value = *(bool*)_position;
            _position += sizeof(bool);
            return value;
        }

        public byte ReadByte()
        {
            if (!CurrentBufferHasEnoughBytes(sizeof(byte)))
                return (byte)ReadOverlapped(sizeof(byte));

            var value = *_position;
            _position += sizeof(byte);
            return value;
        }

        public sbyte ReadSByte()
        {
            if (!CurrentBufferHasEnoughBytes(sizeof(sbyte)))
                return (sbyte)ReadOverlapped(sizeof(sbyte));

            var value = *(sbyte*)_position;
            _position += sizeof(sbyte);
            return value;
        }

        public short ReadInt16()
        {
            if (!CurrentBufferHasEnoughBytes(sizeof(short)))
                return (short)ReadOverlapped(sizeof(short));

            var value = *(short*)_position;
            _position += sizeof(short);
            return value;
        }

        public ushort ReadUInt16()
        {
            if (!CurrentBufferHasEnoughBytes(sizeof(ushort)))
                return (ushort)ReadOverlapped(sizeof(ushort));

            var value = *(ushort*)_position;
            _position += sizeof(ushort);
            return value;
        }

        public int ReadInt32()
        {
            if (!CurrentBufferHasEnoughBytes(sizeof(int)))
                return (int)ReadOverlapped(sizeof(int));

            var value = *(int*)_position;
            _position += sizeof(int);
            return value;
        }

        public uint ReadUInt32()
        {
            if (!CurrentBufferHasEnoughBytes(sizeof(uint)))
                return (uint)ReadOverlapped(sizeof(uint));

            var value = *(uint*)_position;
            _position += sizeof(uint);
            return value;
        }

        public long ReadInt64()
        {
            if (!CurrentBufferHasEnoughBytes(sizeof(long)))
                return (long)ReadOverlapped(sizeof(long));

            var value = *(long*)_position;
            _position += sizeof(long);
            return value;
        }

        public ulong ReadUInt64()
        {
            if (!CurrentBufferHasEnoughBytes(sizeof(ulong)))
                return ReadOverlapped(sizeof(ulong));

            var value = *(ulong*)_position;
            _position += sizeof(ulong);
            return value;
        }

        public float ReadSingle()
        {
            if (!CurrentBufferHasEnoughBytes(sizeof(float)))
            {
                var temp = ReadOverlapped(sizeof(float));
                return *(float*)&temp;
            }

            var value = *(float*)_position;
            _position += sizeof(float);
            return value;
        }

        public double ReadDouble()
        {
            if (!CurrentBufferHasEnoughBytes(sizeof(double)))
            {
                var temp = ReadOverlapped(sizeof(double));
                return *(double*)&temp;
            }

            var value = *(double*)_position;
            _position += sizeof(double);
            return value;
        }

        public decimal ReadDecimal()
        {
            if (!CurrentBufferHasEnoughBytes(sizeof(decimal)))
                return ReadOverlappedDecimal();

            var proxy = new DecimalProxy();

            proxy.lo = *(int*)_position;
            _position += sizeof(int);
            proxy.mid = *(int*)_position;
            _position += sizeof(int);
            proxy.hi = *(int*)_position;
            _position += sizeof(int);
            proxy.flags = *(int*)_position;
            _position += sizeof(int);

            return *(decimal*)&proxy;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct DecimalProxy
        {
            internal int flags;
            internal int hi;
            internal int lo;
            internal int mid;
        }

        public string ReadString()
        {
            var byteCount = ReadVariableLengthQuantity();

            // we rely on the existing ReadBytes method, which is probably not the most efficient
            // implementation, but ReadString should not be called if performance matters anyways
            var bytes = ReadBytes(byteCount);
            if (bytes.Length != byteCount)
                throw CreateInvalidOperationException();

            var value = _encoding.GetString(bytes);
            return value;
        }

        public int ReadVariableLengthQuantity()
        {
            var count = 0;
            var shift = 0;
            byte b;
            do
            {
                if (shift == 5 * 7)
                    throw new FormatException("Too many bytes in what should have been a 7 bit encoded Int32.");

                b = ReadByte();
                count |= (b & 0x7F) << shift;
                shift += 7;
            } while ((b & 0x80) != 0);
            return count;
        }

        public int PeekChar()
        {
            var ptr = _position;
            var read = Read();
            _position = ptr;
            return read;
        }

        public char ReadChar()
        {
            char value;
            var charCount = ReadOverlappedChars(&value, 0, 1);

            if (charCount == 0)
                throw CreateInvalidOperationException();

            return value;
        }

        public int Read()
        {
            char value;
            var charCount = ReadOverlappedChars(&value, 0, 1);
            return charCount != 0 ? value : -1;
        }

        public int Read(char[] buffer, int index, int count)
        {
            fixed (char* pChars = buffer)
            {
                return ReadOverlappedChars(pChars, index, count);
            }
        }

        public char[] ReadChars(int count)
        {
            var buffer = new char[count];
            var readChars = Read(buffer, 0, count);

            if (readChars == buffer.Length)
                return buffer;

            // todo: maybe throw
            // Trim array if we needed
            var copy = new char[readChars];
            Array.Copy(buffer, 0, copy, 0, readChars);
            return copy;
        }

        public int AvailableData()
        {
            return (int)(_endOfBuffer - _position);
        }

        public byte[] ReadRemainingBytes()
        {
            var availableData = (int)(_endOfBuffer - _position);
            return ReadBytes(availableData);
        }

        public byte[] ReadBytes(int count)
        {
            var buffer = new byte[count];
            var readBytes = Read(buffer, 0, count);
            
            if (readBytes == buffer.Length)
                return buffer;

            // todo: maybe throw
            // Trim array if we needed
            var copy = new byte[readBytes];
            Buffer.BlockCopy(buffer, 0, copy, 0, readBytes);
          
            return copy;
        }

        public int Read(byte[] buffer, int index, int count)
        {
            fixed (byte* pBytes = buffer)
            {
                var remainingCount = count;
                while (true)
                {
                    var byteCount = ReadBytesInCurrentBuffer(pBytes, index, remainingCount);
                    remainingCount -= byteCount;
                    if (remainingCount == 0 || _bufferIndex == _bufferCount)
                        return count - remainingCount;

                    SwithToNextBuffer();
                    index += byteCount;
                }
            }
        }

        private int ReadBytesInCurrentBuffer(byte* pBytes, int index, int remainingCount)
        {
            var availableData = (int)(_endOfBuffer - _position);
            var bytesToRead = Math.Min(remainingCount, availableData);
            Buffer.MemoryCopy(_position, pBytes + index, bytesToRead, bytesToRead);
            _position += bytesToRead;
            return bytesToRead;
        }

        private int ReadOverlappedChars(char* pChars, int index, int count)
        {
            var remainingCount = count;
            while (true)
            {
                var charCount = ReadCharsInCurrentBuffer(pChars, index, remainingCount);

                remainingCount -= charCount;
                if (remainingCount == 0 || _bufferIndex == _bufferCount)
                    return count - remainingCount;

                SwithToNextBuffer();
                index += charCount;
            }
        }

        private int ReadCharsInCurrentBuffer(char* pChars, int index, int count)
        {
            const int maxCharBytesSize = 128;
            var byteBuffer = stackalloc byte[maxCharBytesSize];
            var bufferOffset = 0;
            var is2BytesPerChar = _encoding is UnicodeEncoding;

            var charsRemaining = count;

            while (charsRemaining > 0)
            {
                // We really want to know what the minimum number of bytes per char
                // is for our encoding.  Otherwise for UnicodeEncoding we'd have to
                // do ~1+log(n) reads to read n characters. 
                var numBytes = charsRemaining;

                // Special case for UTF8Decoder when there are residual bytes from previous loop 
                if (HasDecoderStateFunction(_decoder) && numBytes > 1)
                    --numBytes;

                if (is2BytesPerChar)
                    numBytes <<= 1;

                if (numBytes > maxCharBytesSize)
                    numBytes = maxCharBytesSize;

                if (_position + numBytes > _endOfBuffer)
                {
                    numBytes = (int)(_endOfBuffer - _position);

                    // Super special case: having an odd number of available bytes and needing
                    // more than one byte per char. The decoder would swallow the extra byte
                    // and we would incorrectly advance the current position
                    if (is2BytesPerChar && numBytes % 2 != 0)
                    {
                        if (numBytes != 1)
                        {
                            // We just act like if there was one byte less to be sure that all 
                            // bytes read by the decoder will be consumed for a reason
                            --numBytes;
                        }
                        else
                        {
                            // If there was only one byte left, we need to move it to the stackallocated buffer to not lose it
                            // then switch the current buffer remembering we already have moved that byte
                            *byteBuffer = *_position;
                            bufferOffset = 1;

                            do
                            {
                                SwithToNextBuffer();
                            } while (_position == _endOfBuffer); // skipping empty buffers
                        }
                    }
                }

                if (numBytes == 0)
                    return count - charsRemaining;

                Buffer.MemoryCopy(_position, byteBuffer + bufferOffset, maxCharBytesSize, numBytes);
                _position += numBytes;
                var charsRead = _decoder.GetChars(byteBuffer, numBytes + bufferOffset, pChars + index, charsRemaining, false);
                bufferOffset = 0;

                charsRemaining -= charsRead;
                index += charsRead;
            }
            if (HasDecoderStateFunction(_decoder))
                _position += 1;

            // We may have read fewer than the number of characters requested if end of stream reached 
            // or if the encoding makes the char count too big for the buffer (e.g. fallback sequence)
            return count - charsRemaining;
        }

   

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool CurrentBufferHasEnoughBytes(int size)
        {
            return _position + size <= _endOfBuffer;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static InvalidOperationException CreateInvalidOperationException()
        {
            return new InvalidOperationException("Unable to read past current buffer length");
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void SwithToNextBuffer()
        {
            if (_bufferIndex == _bufferCount)
                throw CreateInvalidOperationException();

            var buffer = _buffers[_bufferIndex++];
            _position = buffer.Data;
            _endOfBuffer = _position + buffer.Length;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private decimal ReadOverlappedDecimal()
        {
            var proxy = new DecimalProxy
            {
                lo = (int)ReadOverlapped(sizeof(int)),
                mid = (int)ReadOverlapped(sizeof(int)),
                hi = (int)ReadOverlapped(sizeof(int)),
                flags = (int)ReadOverlapped(sizeof(int))
            };

            return *(decimal*)&proxy;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private ulong ReadOverlapped(int sizeOfType)
        {
            var byteRead = 0;
            var value = 0ul;
            while (byteRead < sizeOfType)
            {
                if (_position == _endOfBuffer)
                    SwithToNextBuffer();

                value |= (ulong)*_position++ << (byteRead++ * 8);
            }
            return value;
        }

        private static Func<Decoder, bool> BuildHasDecoderStateFunction()
        {
            var arg = Expression.Parameter(typeof(Decoder));
            var utf8DecoderType = typeof(UTF8Encoding).GetNestedType("UTF8Decoder", BindingFlags.NonPublic | BindingFlags.Public);
            var utf8DecoderHasStateProp = utf8DecoderType.GetProperty("HasState", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);

            var utf7DecoderType = typeof(UTF7Encoding).GetNestedType("Decoder", BindingFlags.NonPublic | BindingFlags.Public);
            var utf7DecoderHasStateProp = utf7DecoderType.GetProperty("HasState", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);

            var utf8Decoder = Expression.Parameter(utf8DecoderType);
            var utf7Decoder = Expression.Parameter(utf7DecoderType);

            var block = Expression.Block(
                new[] { utf8Decoder, utf7Decoder },
                Expression.Assign(utf8Decoder, Expression.TypeAs(arg, utf8DecoderType)),
                Expression.Assign(utf7Decoder, Expression.TypeAs(arg, utf7DecoderType)),
                Expression.Condition(Expression.ReferenceNotEqual(utf8Decoder, Expression.Constant(null, utf8DecoderType)),
                    Expression.Property(utf8Decoder, utf8DecoderHasStateProp),
                    Expression.Condition(Expression.ReferenceNotEqual(utf7Decoder, Expression.Constant(null, utf7DecoderType)),
                        Expression.Property(utf7Decoder, utf7DecoderHasStateProp),
                        Expression.Constant(false))));

            return Expression.Lambda<Func<Decoder, bool>>(block, arg).Compile();
        }
    }
}
