using System;
using System.Collections.Generic;

namespace Sachiel.Extensions.Binary
{
    internal static class BinaryExtensions
    {
        /// <summary>
        ///     Writes the specified integer as a 7-bit encoded variable-length quantity.
        /// </summary>
        /// <param name="writer"></param>
        /// <param name="integer"></param>
        public static byte[] EncodeVariableLengthQuantity(ulong integer)
        {
            if (integer > Math.Pow(2, 56))
                throw new OverflowException("Integer exceeds max value.");

            var results = new List<byte>();
            var index = 3;
            var significantBitReached = false;
            var mask = 0x7fUL << (index * 7);
            while (index >= 0)
            {
                var buffer = mask & integer;
                if (buffer > 0 || significantBitReached)
                {
                    significantBitReached = true;
                    buffer >>= index * 7;
                    if (index > 0)
                        buffer |= 0x80;
                    results.Add((byte) buffer);
                }

                mask >>= 7;
                index--;
            }

            if (!significantBitReached && index < 0)
                results.Add(new byte());
            return results.ToArray();
        }
    }
}