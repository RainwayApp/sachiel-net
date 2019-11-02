namespace Sachiel.Messages.Security
{
    /// <summary>
    ///     A Cyclic Redundancy Check (CRC) is an error-detecting code commonly used in digital networks and storage devices to
    ///     detect accidental changes to raw data.
    ///     While network protocols like TCP And UDP have built in checksums, we may wish to check data integrity at a software
    ///     level.
    /// </summary>
    internal static unsafe class Crc32
    {
        /// <summary>
        ///     Computes the CRC32 checksum of a serialized Sachiel message
        /// </summary>
        /// <param name="source"></param>
        /// <returns></returns>
        public static uint Compute(byte[] source)
        {
            fixed (byte* pSource = source)
            {
                return Compute(pSource, source.Length);
            }
        }

        /// <summary>
        ///     Computes the CRC32 checksum of a serialized Sachiel message via a pointer.
        /// </summary>
        /// <param name="bytes"></param>
        /// <param name="length"></param>
        /// <returns></returns>
        public static uint Compute(byte* bytes, int length)
        {
            const uint poly = 0xedb88320;
            const int tableLength = 256;

            var table = stackalloc uint[tableLength];

            for (uint tableIndex = 0; tableIndex < tableLength; ++tableIndex)
            {
                var temp = tableIndex;
                for (var remainder = 8; remainder > 0; --remainder)
                    if ((temp & 1) == 1)
                        temp = (temp >> 1) ^ poly;
                    else
                        temp >>= 1;
                table[tableIndex] = temp;
            }

            var crc = 0xffffffff;
            for (var i = 0; i < length; ++i)
            {
                var index = (byte) ((crc & 0xff) ^ bytes[i]);
                crc = (crc >> 8) ^ table[index];
            }

            return ~crc;
        }
    }
}