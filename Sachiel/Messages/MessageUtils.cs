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

        /// <summary>
        ///  We use reflection to get the Proto Schema from protobuff-net
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public static string GetSchema(Type type, ProtoSyntax syntax = ProtoSyntax.Proto2)
        {
            var method = typeof(Serializer).GetMethod("GetProto", new Type[] { typeof(ProtoSyntax) }).MakeGenericMethod(type);
            method.Invoke(null, new object[] { syntax });
            return (string) method.Invoke(null, new object[] { syntax });
        }
    }
}