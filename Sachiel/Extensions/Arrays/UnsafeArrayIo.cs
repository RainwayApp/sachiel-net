using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Threading.Tasks;

namespace Sachiel.Extensions.Arrays
{
    /// <summary>
    ///     Allows fast, unsafe blitting of structure arrays to and from streams, similar to what you can do in C/C++. Because
    ///     of
    ///     https://en.wikipedia.org/wiki/Endianness , the written data is not guaranteed to be compatible between processor
    ///     architectures --
    ///     in particular, it's not compatible with regular <see cref="BinaryReader" /> and <see cref="BinaryWriter" />, nor
    ///     with many
    ///     network protocols. It can provide a gargantuan performance advantage against BinaryReader/BinaryWriter in certain
    ///     cases, just
    ///     be sure it's both written and read using this class.
    ///     USE AT YOUR OWN RISK!
    /// </summary>
    public static class UnsafeArrayIo
    {
        /// <summary>
        ///     Cache of converters we've generated.
        /// </summary>
        private static readonly Dictionary<Type, ArrayConverter> Converters = new Dictionary<Type, ArrayConverter>();


        public static byte[] SerializeToByteArray(this object obj)
        {
            if (obj == null) return null;
            var bf = new BinaryFormatter();
            using (var ms = new MemoryStream())
            {
                bf.Serialize(ms, obj);
                return ms.ToArray();
            }
        }

        public static async Task<T> Deserialize<T>(this byte[] byteArray) where T : class
        {
            if (byteArray == null) return null;
            using (var memStream = new MemoryStream())
            {
                var binForm = new BinaryFormatter();
                await memStream.WriteAsync(byteArray, 0, byteArray.Length);
                memStream.Seek(0, SeekOrigin.Begin);
                var obj = (T) binForm.Deserialize(memStream);
                return obj;
            }
        }


        /// <summary>
        ///     Reads an array of type T[] directly from the stream without doing any processing. Assumes the endianess of the
        ///     values
        ///     are the same as the processor (which means you cannot have used a regular <see cref="BinaryWriter" /> to write it,
        ///     since
        ///     that writes big-endian on x86/x64, which are little-endian processors). Does not do any type checking; simply blits
        ///     the
        ///     memory.
        /// </summary>
        /// <typeparam name="T">The array type (used in a fixed() expression -- so it must not contain any managed references).</typeparam>
        /// <param name="stream">Stream to read from.</param>
        /// <param name="elementCount">Number of elements to read (not the number of bytes -- to read 2 ints, pass 2, not 8).</param>
        /// <returns>The correctly typed array.</returns>
        public static async Task<T[]> ReadArray<T>(Stream stream, int elementCount) where T : struct
        {
            if (stream == null) throw new ArgumentNullException(nameof(stream));
            if (elementCount <= 0) return new T[0];
            var converter = GetConverter<T>();
            var nBytes = elementCount * converter.SizeOf;
            byte[] buffer;
            if (converter.UseDoubleHack)
            {
                var doubles = new double[elementCount];
                buffer = converter.ConvertToByte(doubles, nBytes);
            }
            else
            {
                buffer = new byte[nBytes];
            }

            await stream.ReadAsync(buffer, 0, nBytes);
            return (T[]) converter.ConvertFromByte(buffer, elementCount);
        }

        /// <summary>
        ///     Reads an array of type T[] directly from the stream without doing any processing. Assumes the endianess of the
        ///     values
        ///     are the same as the processor (which means you cannot have used a regular <see cref="BinaryWriter" /> to write it,
        ///     since
        ///     that writes big-endian on x86/x64, which are little-endian processors). Does not do any type checking; simply blits
        ///     the
        ///     memory.
        /// </summary>
        /// <typeparam name="T">The array type (used in a fixed() expression -- so it must not contain any managed references).</typeparam>
        /// <param name="stream">Stream to read from.</param>
        /// <param name="elementCount">Number of elements to read (not the number of bytes -- to read 2 ints, pass 2, not 8).</param>
        /// <returns>The correctly typed array.</returns>
        public static T[] ReadArray<T>(BinaryReader stream, int elementCount) where T : struct
        {
            if (stream == null) throw new ArgumentNullException(nameof(stream));
            if (elementCount <= 0) return new T[0];
            var converter = GetConverter<T>();
            var nBytes = elementCount * converter.SizeOf;
            byte[] buffer;
            if (converter.UseDoubleHack)
            {
                var doubles = new double[elementCount];
                buffer = converter.ConvertToByte(doubles, nBytes);
            }
            else
            {
                buffer = new byte[nBytes];
            }

            stream.Read(buffer, 0, nBytes);
            return (T[]) converter.ConvertFromByte(buffer, elementCount);
        }

        /// <summary>
        ///     Reads an array of type T[] directly from the stream without doing any processing. Writes values with the same
        ///     endianess
        ///     as the processor (which means you cannot use a regular <see cref="BinaryReader" /> to read it, since that reads
        ///     big-endian
        ///     on x86/x64, which are little-endian processors). Does not do any type checking; simply blits the memory.
        /// </summary>
        /// <typeparam name="T">The array type (used in a fixed() expression -- so it must not contain any managed references).</typeparam>
        /// <param name="stream">Stream to write to.</param>
        /// <param name="array">Array to write.</param>
        /// <param name="isThreadSafe">
        ///     If you are 200% absolutely sure no other threads will access the array, you can set this to true
        ///     to prevent a temporary copy from being made. If you set this to true and another thread is accessing the array, the
        ///     runtime
        ///     will likely crash or unexpected behavior will happen. If even a tiny bit unsure, leave this as false.
        /// </param>
        public static async Task WriteArray<T>(Stream stream, T[] array, bool isThreadSafe = false) where T : struct
        {
            if (stream == null) throw new ArgumentNullException(nameof(stream));
            if (array == null || array.Length == 0) return;
            if (array.GetLowerBound(0) != 0) throw new InvalidOperationException("Array lower bound must be 0");

            if (!isThreadSafe)
            {
                // need to create a duplicate of the array
                var copy = new T[array.Length];
                Array.Copy(array, copy, array.Length);
                array = copy;
            }

            var converter = GetConverter<T>();
            var elementCount = array.Length;
            var nBytes = elementCount * converter.SizeOf;
            var buffer = converter.ConvertToByte(array, nBytes);
            try
            {
                await stream.WriteAsync(buffer, 0, buffer.Length);
            }
            finally
            {
                if (isThreadSafe)
                    // If we changed the original array type, change it back.
                    converter.ConvertFromByte(buffer, elementCount);
            }
        }

        /// <summary>
        ///     Reads an array of type T[] directly from the stream without doing any processing. Writes values with the same
        ///     endianess
        ///     as the processor (which means you cannot use a regular <see cref="BinaryReader" /> to read it, since that reads
        ///     big-endian
        ///     on x86/x64, which are little-endian processors). Does not do any type checking; simply blits the memory.
        /// </summary>
        /// <typeparam name="T">The array type (used in a fixed() expression -- so it must not contain any managed references).</typeparam>
        /// <param name="stream">Stream to write to.</param>
        /// <param name="array">Array to write.</param>
        /// <param name="isThreadSafe">
        ///     If you are 200% absolutely sure no other threads will access the array, you can set this to true
        ///     to prevent a temporary copy from being made. If you set this to true and another thread is accessing the array, the
        ///     runtime
        ///     will likely crash or unexpected behavior will happen. If even a tiny bit unsure, leave this as false.
        /// </param>
        public static void WriteArray<T>(BinaryWriter stream, T[] array, bool isThreadSafe = false) where T : struct
        {
            if (stream == null) throw new ArgumentNullException(nameof(stream));
            if (array == null || array.Length == 0) return;
            if (array.GetLowerBound(0) != 0) throw new InvalidOperationException("Array lower bound must be 0");

            if (!isThreadSafe)
            {
                // need to create a duplicate of the array
                var copy = new T[array.Length];
                Array.Copy(array, copy, array.Length);
                array = copy;
            }

            var converter = GetConverter<T>();
            var elementCount = array.Length;
            var nBytes = elementCount * converter.SizeOf;
            var buffer = converter.ConvertToByte(array, nBytes);
            try
            {
                stream.Write(buffer, 0, buffer.Length);
            }
            finally
            {
                if (isThreadSafe)
                    // If we changed the original array type, change it back.
                    converter.ConvertFromByte(buffer, elementCount);
            }
        }

        /// <summary>
        ///     Gets or creates a converter for the given type.
        /// </summary>
        private static ArrayConverter GetConverter<T>()
        {
            var type = typeof(T);
            if (!Converters.TryGetValue(type, out var result))
            {
                result = new ArrayConverter(type, new T[1]);
                Converters[type] = result;
            }

            return result;
        }
    }
}