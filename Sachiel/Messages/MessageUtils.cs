using System;
using System.IO;
using System.Reflection;
using ProtoBuf;
using ProtoBuf.Meta;
using Sachiel.Messages.Packets;

namespace Sachiel.Messages
{
    internal static class MessageUtils
    {

        public static unsafe T Deserialize<T>(byte* data, int length)
        {
            using (var ms = new UnmanagedMemoryStream(data, length))
            {
                return Serializer.Deserialize<T>(ms);
            }
        }

        // TODO Find a better way?
        public static unsafe T Deserialize<T>(ReadOnlySpan<byte> data)
        {
            using (var ms = new MemoryStream(data.ToArray())) {
                return Serializer.Deserialize<T>(ms);
            }
        }

        /// <summary>
        ///  We use reflection to get the Proto Schema from protobuff-net
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public static string GetSchema(Type type)
        {
            var method = typeof(Serializer).GetMethod("GetProto", Type.EmptyTypes).MakeGenericMethod(type);
            method.Invoke(null, null);
            return (string) method.Invoke(null, null);
        }
    }
}