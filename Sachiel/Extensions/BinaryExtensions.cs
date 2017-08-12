using System;
using System.IO;

namespace Sachiel.Extensions
{
    internal static class BinaryExtensions
    {
        /// <summary>
        /// Reads a 7-bit encoded variable-length quantity from binary and return it as integer.
        /// </summary>
        /// <returns></returns>
        public static uint ReadVariableLengthQuantity(this BinaryReader reader)
        {
            var index = 0;
            uint buffer = 0;
            byte current;
            do
            {
                if (index++ == 8)
                    throw new FormatException("Could not read variable-length quantity from provided stream.");

                buffer <<= 7;

                current = reader.ReadByte();
                buffer |= (current & 0x7FU);
            } while ((current & 0x80) != 0);

            return buffer;
        }

        /// <summary>
        /// Writes the specified integer as a 7-bit encoded variable-length quantity.
        /// </summary>
        /// <param name="writer"></param>
        /// <param name="integer"></param>
        public static void WriteVariableLengthQuantity(this BinaryWriter writer, ulong integer)
        {
            if (integer > Math.Pow(2, 56))
                throw new OverflowException("Integer exceeds max value.");

            var index = 3;
            var significantBitReached = false;
            var mask = 0x7fUL << (index * 7);
            while (index >= 0)
            {
                var buffer = (mask & integer);
                if (buffer > 0 || significantBitReached)
                {
                    significantBitReached = true;
                    buffer >>= index * 7;
                    if (index > 0)
                        buffer |= 0x80;
                    writer.Write((byte)buffer);
                }
                mask >>= 7;
                index--;
            }

            if (!significantBitReached && index < 0)
                writer.Write(new byte());
        }
    }
}
